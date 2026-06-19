using Domain.Enums;

namespace Domain.Entities;

public class ProblemReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ReporterUserId { get; set; }
    public Guid? ZoneId { get; set; }
    public string? SlotCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProblemStatus Status { get; set; } = ProblemStatus.Open;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAtUtc { get; set; }
}
