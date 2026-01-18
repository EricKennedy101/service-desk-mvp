using System.ComponentModel.DataAnnotations;
using FRAServiceRequestPortal.Domain.Enums;

namespace FRAServiceRequestPortal.Contracts.Requests;

public class CreateCaseRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string? Category { get; set; }

    public CasePriority? Priority { get; set; }

    public CaseSeverity? Severity { get; set; }

    public string? CreatedByEmail { get; set; }

    public string? AssignedToEmail { get; set; }

    public string? SourceSystem { get; set; }

    public List<string>? Tags { get; set; }
}
