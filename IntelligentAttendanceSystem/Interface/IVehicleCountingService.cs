using IntelligentAttendanceSystem.Models;

namespace IntelligentAttendanceSystem.Interface
{
    public interface IVehicleCountingService
    {
        Task CountVehicleAsync(int junctionId, string vehicleType, string direction, int vehicleSize, string plateNumber, int speed, int confidence);
        Task<VehicleCountingStats> GetCountingStatsAsync(DateTime startTime, DateTime endTime, int? junctionId = null);
        Task<List<VehicleCount>> GetHourlyCountsAsync(DateTime date, int? junctionId = null);
        Task ResetCountsAsync();
        event EventHandler<VehicleDetectionEvent> OnVehicleDetected;
    }
}
