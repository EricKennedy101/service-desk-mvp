using FRAServiceRequestPortal.Contracts.Requests;
using FRAServiceRequestPortal.Domain.Entities;
using FRAServiceRequestPortal.Domain.Enums;
using FRAServiceRequestPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Security.Claims;

namespace FRAServiceRequestPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SOCAnalyst,SOCLead,Admin")]
public class CasesController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    private readonly long _maxEvidenceSizeBytes;
    private readonly HashSet<string> _allowedExtensions;
    private readonly string _evidenceRootPath;

    private static readonly Dictionary<string, HashSet<string>> AllowedMimeTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        [".txt"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "text/plain" },
        [".log"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "text/plain" },
        [".csv"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "text/csv", "application/vnd.ms-excel" },
        [".json"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/json", "text/json" },
        [".png"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/png" },
        [".jpg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg" },
        [".jpeg"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "image/jpeg" },
        [".pdf"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/pdf" },
        [".docx"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        [".xlsx"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        [".pptx"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        [".zip"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "application/zip", "application/x-zip-compressed" }
    };

    public CasesController(AppDbContext dbContext, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _environment = environment;
        _maxEvidenceSizeBytes = configuration.GetValue<long?>("EvidenceUpload:MaxSizeBytes") ?? 20 * 1024 * 1024;
        var configuredExtensions = configuration.GetSection("EvidenceUpload:AllowedExtensions").Get<List<string>>();
        _allowedExtensions = configuredExtensions is { Count: > 0 }
            ? new HashSet<string>(configuredExtensions.Select(e => e.ToLowerInvariant()), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".txt", ".log", ".csv", ".json", ".png", ".jpg", ".jpeg", ".pdf", ".docx", ".xlsx", ".pptx", ".zip"
            };
        _evidenceRootPath = configuration["EvidenceUpload:RootPath"] ?? "EvidenceUploads";
    }

    [HttpGet]
    public async Task<ActionResult<List<Case>>> GetCases(
        [FromQuery] CaseStatus? status,
        [FromQuery] CasePriority? priority,
        [FromQuery] CaseSeverity? severity,
        [FromQuery] string? assignedToEmail,
        [FromQuery] string? createdByEmail,
        [FromQuery] string? sourceSystem,
        [FromQuery] string? tag,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1)
        {
            page = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        var query = _dbContext.Cases.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(c => c.Priority == priority.Value);
        }

        if (severity.HasValue)
        {
            query = query.Where(c => c.Severity == severity.Value);
        }

        if (!string.IsNullOrWhiteSpace(assignedToEmail))
        {
            query = query.Where(c => c.AssignedToEmail == assignedToEmail);
        }

        if (!string.IsNullOrWhiteSpace(createdByEmail))
        {
            query = query.Where(c => c.CreatedByEmail == createdByEmail);
        }

        if (!string.IsNullOrWhiteSpace(sourceSystem))
        {
            query = query.Where(c => c.SourceSystem == sourceSystem);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            query = query.Where(c => c.Tags != null && c.Tags.Contains(tag));
        }

        if (from.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(c => c.CreatedAt <= to.Value);
        }

        var cases = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(cases);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Case>> GetCaseById(int id, [FromQuery] bool includeDeleted = false)
    {
        var caseItem = await _dbContext.Cases.FindAsync(id);
        if (caseItem is null)
        {
            return NotFound();
        }

        if (caseItem.IsDeleted && !includeDeleted)
        {
            return NotFound();
        }

        return Ok(caseItem);
    }

    [HttpPost]
    public async Task<ActionResult<Case>> CreateCase([FromBody] CreateCaseRequest request)
    {
        var caseItem = new Case
        {
            Title = request.Title,
            Description = request.Description,
            Category = request.Category,
            Priority = request.Priority,
            Severity = request.Severity,
            AssignedToEmail = request.AssignedToEmail,
            SourceSystem = request.SourceSystem,
            Tags = request.Tags?.ToList() ?? new List<string>(),
            CreatedByEmail = request.CreatedByEmail,
            Status = CaseStatus.New,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Cases.Add(caseItem);
        await _dbContext.SaveChangesAsync();
        AddCaseEvent(caseItem.Id, "Created", null, null, null, GetActorEmail());
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCaseById), new { id = caseItem.Id }, caseItem);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Case>> UpdateCase(
        int id,
        [FromBody] UpdateCaseRequest request)
    {
        var normalizedActorEmail = GetActorEmail();
        var caseItem = await _dbContext.Cases.FindAsync(id);
        if (caseItem is null)
        {
            return NotFound();
        }

        if (request.Title is not null && request.Title != caseItem.Title)
        {
            AddCaseEvent(caseItem.Id, "Updated", "Title", caseItem.Title, request.Title, normalizedActorEmail);
            caseItem.Title = request.Title;
        }

        if (request.Description is not null && request.Description != caseItem.Description)
        {
            AddCaseEvent(caseItem.Id, "Updated", "Description", caseItem.Description, request.Description, normalizedActorEmail);
            caseItem.Description = request.Description;
        }

        if (request.Category is not null && request.Category != caseItem.Category)
        {
            AddCaseEvent(caseItem.Id, "Updated", "Category", caseItem.Category, request.Category, normalizedActorEmail);
            caseItem.Category = request.Category;
        }

        if (request.Priority is not null && request.Priority != caseItem.Priority)
        {
            AddCaseEvent(caseItem.Id, "Updated", "Priority", caseItem.Priority?.ToString(), request.Priority?.ToString(), normalizedActorEmail);
            caseItem.Priority = request.Priority;
        }

        if (request.Status is not null && request.Status != caseItem.Status)
        {
            AddCaseEvent(caseItem.Id, "Updated", "Status", caseItem.Status.ToString(), request.Status.ToString(), normalizedActorEmail);
            caseItem.Status = request.Status.Value;
        }

        if (request.Severity is not null && request.Severity != caseItem.Severity)
        {
            AddCaseEvent(caseItem.Id, "Updated", "Severity", caseItem.Severity?.ToString(), request.Severity?.ToString(), normalizedActorEmail);
            caseItem.Severity = request.Severity;
        }

        if (request.AssignedToEmail is not null && request.AssignedToEmail != caseItem.AssignedToEmail)
        {
            AddCaseEvent(caseItem.Id, "Updated", "AssignedToEmail", caseItem.AssignedToEmail, request.AssignedToEmail, normalizedActorEmail);
            caseItem.AssignedToEmail = request.AssignedToEmail;
        }

        if (request.SourceSystem is not null && request.SourceSystem != caseItem.SourceSystem)
        {
            AddCaseEvent(caseItem.Id, "Updated", "SourceSystem", caseItem.SourceSystem, request.SourceSystem, normalizedActorEmail);
            caseItem.SourceSystem = request.SourceSystem;
        }

        if (request.Tags is not null)
        {
            var existingTags = caseItem.Tags ?? new List<string>();
            if (!existingTags.SequenceEqual(request.Tags))
            {
                AddCaseEvent(
                    caseItem.Id,
                    "Updated",
                    "Tags",
                    string.Join(",", existingTags),
                    string.Join(",", request.Tags),
                    normalizedActorEmail);
                caseItem.Tags = request.Tags.ToList();
            }
        }

        if (request.CreatedByEmail is not null && request.CreatedByEmail != caseItem.CreatedByEmail)
        {
            AddCaseEvent(caseItem.Id, "Updated", "CreatedByEmail", caseItem.CreatedByEmail, request.CreatedByEmail, normalizedActorEmail);
            caseItem.CreatedByEmail = request.CreatedByEmail;
        }

        await _dbContext.SaveChangesAsync();
        return Ok(caseItem);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SOCLead,Admin")]
    public async Task<IActionResult> DeleteCase(int id)
    {
        var caseItem = await _dbContext.Cases.FindAsync(id);
        if (caseItem is null)
        {
            return NotFound();
        }

        var actorEmail = GetActorEmail();
        AddCaseEvent(caseItem.Id, "Deleted", "IsDeleted", "false", "true", actorEmail);
        caseItem.IsDeleted = true;
        caseItem.DeletedAt = DateTime.UtcNow;
        caseItem.DeletedByEmail = actorEmail;
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id:int}/events")]
    public async Task<ActionResult<List<CaseEvent>>> GetCaseEvents(int id)
    {
        var exists = await _dbContext.Cases.AnyAsync(c => c.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        var events = await _dbContext.CaseEvents
            .Where(e => e.CaseId == id)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        return Ok(events);
    }

    [HttpPost("{id:int}/evidence")]
    public async Task<ActionResult<CaseEvidence>> UploadEvidence(int id, [FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required and must be non-empty." });
        }

        if (file.Length > _maxEvidenceSizeBytes)
        {
            return BadRequest(new { message = $"File exceeds {_maxEvidenceSizeBytes} bytes limit." });
        }

        var caseExists = await _dbContext.Cases.AnyAsync(c => c.Id == id);
        if (!caseExists)
        {
            return NotFound();
        }

        var safeFileName = Path.GetFileName(file.FileName);
        var extension = NormalizeExtension(safeFileName);

        if (!_allowedExtensions.Contains(extension))
        {
            return BadRequest(new
            {
                message = $"File type '{extension}' is not allowed. Allowed extensions: {string.Join(", ", _allowedExtensions.OrderBy(e => e))}"
            });
        }

        if (!IsMimeTypeAllowed(extension, file.ContentType))
        {
            return BadRequest(new { message = "File ContentType does not match the file extension." });
        }
        var actorEmail = GetActorEmail() ?? string.Empty;

        var evidence = new CaseEvidence
        {
            CaseId = id,
            FileName = safeFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            StoragePath = string.Empty,
            UploadedAt = DateTime.UtcNow,
            UploadedByEmail = actorEmail
        };

        _dbContext.CaseEvidence.Add(evidence);
        await _dbContext.SaveChangesAsync();

        var evidenceDirectory = Path.Combine(_environment.ContentRootPath, _evidenceRootPath, id.ToString());
        Directory.CreateDirectory(evidenceDirectory);

        var storagePath = Path.Combine(evidenceDirectory, $"{evidence.Id}_{safeFileName}");
        string? sha256;

        await using (var input = file.OpenReadStream())
        await using (var output = System.IO.File.Create(storagePath))
        {
            using var hasher = SHA256.Create();
            sha256 = await CopyToWithHashAsync(input, output, hasher);
        }

        evidence.StoragePath = storagePath;
        evidence.Sha256 = sha256;

        AddCaseEvent(evidence.CaseId, "EvidenceUploaded", "Evidence", null, evidence.FileName, actorEmail);
        await _dbContext.SaveChangesAsync();

        return Ok(evidence);
    }

    [HttpGet("{id:int}/evidence")]
    public async Task<ActionResult<List<CaseEvidence>>> GetEvidenceList(int id)
    {
        var caseExists = await _dbContext.Cases.AnyAsync(c => c.Id == id);
        if (!caseExists)
        {
            return NotFound();
        }

        var evidence = await _dbContext.CaseEvidence
            .Where(e => e.CaseId == id)
            .OrderByDescending(e => e.UploadedAt)
            .ToListAsync();

        return Ok(evidence);
    }

    [HttpGet("{id:int}/evidence/{evidenceId:int}")]
    public async Task<IActionResult> DownloadEvidence(int id, int evidenceId)
    {
        var evidence = await _dbContext.CaseEvidence
            .FirstOrDefaultAsync(e => e.Id == evidenceId && e.CaseId == id);

        if (evidence is null)
        {
            return NotFound();
        }

        if (!System.IO.File.Exists(evidence.StoragePath))
        {
            return NotFound();
        }

        var stream = new FileStream(evidence.StoragePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, evidence.ContentType, evidence.FileName);
    }

    private void AddCaseEvent(int caseId, string eventType, string? fieldName, string? oldValue, string? newValue, string? actorEmail)
    {
        _dbContext.CaseEvents.Add(new CaseEvent
        {
            CaseId = caseId,
            EventType = eventType,
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ActorEmail = actorEmail,
            CreatedAt = DateTime.UtcNow
        });
    }

    private string? GetActorEmail()
    {
        var claimEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(claimEmail))
        {
            return claimEmail.Trim();
        }

        if (Request.Headers.TryGetValue("X-Actor-Email", out var headerValue))
        {
            var fallback = headerValue.ToString();
            return string.IsNullOrWhiteSpace(fallback) ? null : fallback.Trim();
        }

        return null;
    }

    private static string NormalizeExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return string.IsNullOrWhiteSpace(extension) ? string.Empty : extension.ToLowerInvariant();
    }

    private static bool IsMimeTypeAllowed(string extension, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return true;
        }

        if (!AllowedMimeTypesByExtension.TryGetValue(extension, out var allowed))
        {
            return true;
        }

        return allowed.Contains(contentType);
    }

    private static async Task<string> CopyToWithHashAsync(Stream input, Stream output, HashAlgorithm hasher)
    {
        var buffer = new byte[81920];
        int bytesRead;

        while ((bytesRead = await input.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            hasher.TransformBlock(buffer, 0, bytesRead, null, 0);
            await output.WriteAsync(buffer.AsMemory(0, bytesRead));
        }

        hasher.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(hasher.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
    }
}
