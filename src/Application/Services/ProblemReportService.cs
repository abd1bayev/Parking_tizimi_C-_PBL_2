using Application.Common;
using Application.DTOs.Problems;
using Application.Interfaces;
using Application.Internal;
using Domain.Entities;
using Domain.Enums;

namespace Application.Services;

public sealed class ProblemReportService : IProblemReportService
{
    private readonly IClock _clock;
    private readonly IParkingStateStore _stateStore;

    public ProblemReportService(IParkingStateStore stateStore, IClock clock)
    {
        _stateStore = stateStore;
        _clock = clock;
    }

    public OperationResult<ProblemReportDto> Report(ReportProblemRequest request)
    {
        if (!InputNormalizer.TryNormalizeRequired(request.Title, out var title))
        {
            return OperationResult<ProblemReportDto>.Failure("Muammo sarlavhasi bo'sh bo'lmasligi kerak.");
        }

        if (!InputNormalizer.TryNormalizeRequired(request.Description, out var description))
        {
            return OperationResult<ProblemReportDto>.Failure("Muammo tavsifi bo'sh bo'lmasligi kerak.");
        }

        var state = _stateStore.State;
        ParkingZone? zone = null;
        if (request.ZoneId.HasValue)
        {
            zone = ParkingStateHelper.GetZoneById(state, request.ZoneId.Value);
        }

        var report = new ProblemReport
        {
            ReporterUserId = request.ReporterUserId,
            ZoneId = zone?.Id,
            SlotCode = request.SlotCode?.Trim().ToUpperInvariant(),
            Title = title,
            Description = description,
            Status = ProblemStatus.Open,
            CreatedAtUtc = _clock.UtcNow
        };

        state.ProblemReports.Add(report);
        return OperationResult<ProblemReportDto>.Success(ToDto(report, zone?.Name ?? "Noma'lum"),
            "Muammo xabari qabul qilindi.");
    }

    public IReadOnlyList<ProblemReportDto> GetOpenReports() =>
        MapReports(_stateStore.State.ProblemReports
            .Where(report => report.Status != ProblemStatus.Resolved)
            .OrderByDescending(report => report.CreatedAtUtc));

    public IReadOnlyList<ProblemReportDto> GetAllReports() =>
        MapReports(_stateStore.State.ProblemReports.OrderByDescending(report => report.CreatedAtUtc));

    public OperationResult Resolve(Guid adminUserId, Guid reportId)
    {
        if (!ParkingStateHelper.IsAdmin(_stateStore.State, adminUserId))
        {
            return OperationResult.Failure("Faqat ma'mur muammoni yopishi mumkin.");
        }

        var report = _stateStore.State.ProblemReports.FirstOrDefault(candidate => candidate.Id == reportId);
        if (report is null)
        {
            return OperationResult.Failure("Muammo topilmadi.");
        }

        report.Status = ProblemStatus.Resolved;
        report.ResolvedAtUtc = _clock.UtcNow;
        return OperationResult.Success("Muammo hal qilindi deb belgilandi.");
    }

    private IReadOnlyList<ProblemReportDto> MapReports(IEnumerable<ProblemReport> reports)
    {
        var state = _stateStore.State;
        return reports.Select(report =>
        {
            var zoneName = report.ZoneId.HasValue
                ? ParkingStateHelper.GetZoneById(state, report.ZoneId.Value)?.Name ?? "Noma'lum"
                : "Umumiy";
            return ToDto(report, zoneName);
        }).ToList();
    }

    private static ProblemReportDto ToDto(ProblemReport report, string zoneName) =>
        new()
        {
            Id = report.Id,
            Title = report.Title,
            Description = report.Description,
            ZoneName = zoneName,
            SlotCode = report.SlotCode,
            Status = UiLabels.FormatProblemStatus(report.Status),
            CreatedAtUtc = report.CreatedAtUtc
        };
}
