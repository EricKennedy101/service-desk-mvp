namespace FRAServiceRequestPortal.Domain.Entities;

public class CaseEvent
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string? FieldName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? ActorEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
