using IntelligentAttendanceSystem.Models;
using static IntelligentAttendanceSystem.Models.FaceRecognitionModels;

namespace IntelligentAttendanceSystem.Interface
{
    public interface IReportingService
    {
        Task<FaceRecognitionReport> GetFaceRecognitionReportAsync(ReportRequest request);
        Task<MemoryStream> GenerateExcelReportAsync(ReportRequest request);
        Task<MemoryStream> GeneratePdfReportAsync(ReportRequest request);
        Task<List<FaceAttendanceRecord>> GetAttendanceRecordsAsync(ReportRequest request);
        Task<DashboardStats> GetDashboardStatsAsync(CancellationToken cancellationToken );
    }
}
