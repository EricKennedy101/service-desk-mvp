using System.Security.Claims;
using FRAServiceRequestPortal.Contracts.Requests;
using FRAServiceRequestPortal.Domain.Entities;
using FRAServiceRequestPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FRAServiceRequestPortal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TicketsController : ControllerBase
{
    private static readonly HashSet<string> AllowedPriorities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Low", "Medium", "High", "Urgent"
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Open", "InProgress", "Closed"
    };

    private readonly AppDbContext _dbContext;

    public TicketsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateTicket([FromBody] CreateTicketRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        if (!AllowedPriorities.Contains(request.Priority))
        {
            return BadRequest(new { message = "Priority must be Low, Medium, High, or Urgent." });
        }

        var requesterEmail = GetRequesterEmail() ?? request.RequesterEmail;
        if (string.IsNullOrWhiteSpace(requesterEmail))
        {
            return BadRequest(new { message = "RequesterEmail is required when no authenticated user is present." });
        }

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Category = request.Category.Trim(),
            Priority = request.Priority.Trim(),
            Status = "Open",
            RequesterEmail = requesterEmail.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            TranscriptJson = string.IsNullOrWhiteSpace(request.TranscriptJson) ? null : request.TranscriptJson
        };

        _dbContext.Tickets.Add(ticket);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTicketById), new { id = ticket.Id }, new { id = ticket.Id, status = ticket.Status });
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<Ticket>>> GetMyTickets([FromQuery] string? email)
    {
        var requesterEmail = GetRequesterEmail() ?? email;
        if (string.IsNullOrWhiteSpace(requesterEmail))
        {
            return BadRequest(new { message = "Email is required when no authenticated user is present." });
        }

        var tickets = await _dbContext.Tickets
            .Where(t => t.RequesterEmail == requesterEmail)
            .ToListAsync();

        return Ok(tickets.OrderByDescending(t => t.CreatedAt));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Ticket>> GetTicketById(Guid id)
    {
        var ticket = await _dbContext.Tickets.FirstOrDefaultAsync(t => t.Id == id);
        if (ticket is null)
        {
            return NotFound();
        }

        return Ok(ticket);
    }

    private string? GetRequesterEmail()
    {
        var claimEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
        return string.IsNullOrWhiteSpace(claimEmail) ? null : claimEmail.Trim();
    }
}
