namespace Application.DTOs.Problems;

public sealed class ReportProblemRequest
{
    public Guid? ReporterUserId { get; init; }
    public Guid? ZoneId { get; init; }
    public string? SlotCode { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

public sealed class ProblemReportDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ZoneName { get; init; } = string.Empty;
    public string? SlotCode { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
}
