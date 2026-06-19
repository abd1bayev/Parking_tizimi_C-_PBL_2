using Application.Common;
using Application.DTOs.Problems;

namespace Application.Interfaces;

public interface IProblemReportService
{
    OperationResult<ProblemReportDto> Report(ReportProblemRequest request);
    IReadOnlyList<ProblemReportDto> GetOpenReports();
    IReadOnlyList<ProblemReportDto> GetAllReports();
    OperationResult Resolve(Guid adminUserId, Guid reportId);
}
