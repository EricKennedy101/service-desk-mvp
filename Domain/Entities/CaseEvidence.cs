namespace FRAServiceRequestPortal.Domain.Entities;

public class CaseEvidence
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public string? Sha256 { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public string UploadedByEmail { get; set; } = string.Empty;
}
