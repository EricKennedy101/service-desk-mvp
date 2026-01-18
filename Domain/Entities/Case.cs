using System.ComponentModel.DataAnnotations;
using FRAServiceRequestPortal.Domain.Enums;

namespace FRAServiceRequestPortal.Domain.Entities;

public class Case
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Category { get; set; }

    public CasePriority? Priority { get; set; }

    [Required]
    public CaseStatus Status { get; set; } = CaseStatus.New;

    public CaseSeverity? Severity { get; set; }

    public string? AssignedToEmail { get; set; }

    public string? SourceSystem { get; set; }

    public List<string>? Tags { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByEmail { get; set; }

    public DateTime? FirstResponseAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByEmail { get; set; }
}
