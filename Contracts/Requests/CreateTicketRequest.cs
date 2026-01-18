using System.ComponentModel.DataAnnotations;

namespace FRAServiceRequestPortal.Contracts.Requests;

public class CreateTicketRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Priority { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RequesterEmail { get; set; }

    public string? TranscriptJson { get; set; }
}
