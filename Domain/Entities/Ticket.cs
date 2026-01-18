using System.ComponentModel.DataAnnotations;

namespace FRAServiceRequestPortal.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }

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
    public string Priority { get; set; } = "Medium";

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open";

    [Required]
    [MaxLength(200)]
    public string RequesterEmail { get; set; } = string.Empty;

    [Required]
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? TranscriptJson { get; set; }
}
