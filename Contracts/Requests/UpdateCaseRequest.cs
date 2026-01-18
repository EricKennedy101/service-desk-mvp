using System.ComponentModel.DataAnnotations;
using FRAServiceRequestPortal.Domain.Enums;

namespace FRAServiceRequestPortal.Contracts.Requests;

public class UpdateCaseRequest
{
    [MaxLength(200)]
    [MinLength(1)]
    public string? Title { get; set; }

    [MinLength(1)]
    public string? Description { get; set; }

    public string? Category { get; set; }

    public CasePriority? Priority { get; set; }

    public CaseStatus? Status { get; set; }

    public CaseSeverity? Severity { get; set; }

    public string? CreatedByEmail { get; set; }

    public string? AssignedToEmail { get; set; }

    public string? SourceSystem { get; set; }

    public List<string>? Tags { get; set; }
}
