using IntelligentAttendanceSystem.Data;
using IntelligentAttendanceSystem.Interface;
using IntelligentAttendanceSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace IntelligentAttendanceSystem.Services
{
    public class VehicleCountingService : IVehicleCountingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VehicleCountingService> _logger;
        private readonly ConcurrentDictionary<string, int> _hourlyCounts = new();
        private readonly object _lockObject = new object();

        public event EventHandler<VehicleDetectionEvent> OnVehicleDetected;

        public VehicleCountingService(
            ApplicationDbContext context,
            ILogger<VehicleCountingService> logger)
        {
            _context = context;
            _logger = logger;
            InitializeHourlyCounts();
        }

        // Update the CountVehicleAsync method signature
        public async Task CountVehicleAsync(int junctionId, string vehicleType, string direction,
                                           int vehicleSize, string plateNumber, int speed, int confidence)
        {
            try
            {
                var currentTime = DateTime.Now;
                var hourKey = $"{currentTime:yyyyMMddHH}";
                var vehicleKey = $"{vehicleType}_{direction}";
                var confidenceLevel = GetConfidenceLevel(confidence);

                // Update in-memory counts
                lock (_lockObject)
                {
                    _hourlyCounts.AddOrUpdate(hourKey, 1, (key, count) => count + 1);
                    _hourlyCounts.AddOrUpdate($"{hourKey}_{vehicleKey}", 1, (key, count) => count + 1);
                    _hourlyCounts.AddOrUpdate($"{hourKey}_confidence_{confidenceLevel}", 1, (key, count) => count + 1);
                }

                // Create vehicle detection event
                var vehicleEvent = new VehicleDetectionEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventTime = currentTime,
                    JunctionId = junctionId,
                    VehicleType = vehicleType,
                    Direction = direction,
                    VehicleSize = vehicleSize,
                    PlateNumber = plateNumber,
                    Speed = speed,
                    Confidence = confidence,
                    ConfidenceLevel = confidenceLevel
                };

                // Save to database
                await SaveVehicleCountToDatabase(vehicleEvent);

                // Raise event
                OnVehicleDetected?.Invoke(this, vehicleEvent);

                _logger.LogInformation($"Vehicle counted: {vehicleType} (Confidence: {confidence}%) moving {direction} at junction {junctionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting vehicle");
            }
        }

        private string GetConfidenceLevel(int confidence)
        {
            return confidence switch
            {
                >= 90 => "High",
                >= 70 => "Medium",
                >= 50 => "Low",
                _ => "VeryLow"
            };
        }
        private async Task SaveVehicleCountToDatabase(VehicleDetectionEvent vehicleEvent)
        {
            try
            {
                // Get current hour start time
                var hourStart = new DateTime(vehicleEvent.EventTime.Year, vehicleEvent.EventTime.Month,
                                           vehicleEvent.EventTime.Day, vehicleEvent.EventTime.Hour, 0, 0);

                // Check if record exists for this hour, vehicle type, and direction
                var existingCount = await _context.VehicleCounts
                    .FirstOrDefaultAsync(vc => vc.CountDate == hourStart &&
                                             vc.VehicleType == vehicleEvent.VehicleType &&
                                             vc.Direction == vehicleEvent.Direction &&
                                             vc.JunctionId == vehicleEvent.JunctionId);

                if (existingCount != null)
                {
                    existingCount.Count++;
                    _context.VehicleCounts.Update(existingCount);
                }
                else
                {
                    var vehicleCount = new VehicleCount
                    {
                        CountDate = hourStart,
                        TimePeriod = "Hourly",
                        VehicleType = vehicleEvent.VehicleType,
                        Direction = vehicleEvent.Direction,
                        Count = 1,
                        JunctionId = vehicleEvent.JunctionId,
                        CreatedDate = DateTime.UtcNow
                    };
                    _context.VehicleCounts.Add(vehicleCount);
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving vehicle count to database");
            }
        }

        public async Task<VehicleCountingStats> GetCountingStatsAsync(DateTime startTime, DateTime endTime, int? junctionId = null)
        {
            var stats = new VehicleCountingStats
            {
                StartTime = startTime,
                EndTime = endTime
            };

            try
            {
                var query = _context.VehicleCounts
                    .Where(vc => vc.CountDate >= startTime && vc.CountDate <= endTime);

                if (junctionId.HasValue)
                {
                    query = query.Where(vc => vc.JunctionId == junctionId.Value);
                }

                var counts = await query.ToListAsync();

                // Calculate statistics
                stats.TotalVehicles = counts.Sum(c => c.Count);

                // Vehicle type counts
                stats.VehicleTypeCounts = counts
                    .GroupBy(c => c.VehicleType)
                    .ToDictionary(g => g.Key, g => g.Sum(c => c.Count));

                // Direction counts
                stats.DirectionCounts = counts
                    .GroupBy(c => c.Direction)
                    .ToDictionary(g => g.Key, g => g.Sum(c => c.Count));

                // Type-Direction matrix
                stats.TypeDirectionMatrix = counts
                    .GroupBy(c => c.VehicleType)
                    .ToDictionary(
                        g => g.Key,
                        g => g.GroupBy(c => c.Direction)
                              .ToDictionary(dg => dg.Key, dg => dg.Sum(c => c.Count))
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting counting stats");
            }

            return stats;
        }

        public async Task<List<VehicleCount>> GetHourlyCountsAsync(DateTime date, int? junctionId = null)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1).AddSeconds(-1);

                var query = _context.VehicleCounts
                    .Where(vc => vc.CountDate >= startOfDay && vc.CountDate <= endOfDay);

                if (junctionId.HasValue)
                {
                    query = query.Where(vc => vc.JunctionId == junctionId.Value);
                }

                return await query
                    .OrderBy(vc => vc.CountDate)
                    .ThenBy(vc => vc.VehicleType)
                    .ThenBy(vc => vc.Direction)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hourly counts");
                return new List<VehicleCount>();
            }
        }

        public async Task ResetCountsAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    _hourlyCounts.Clear();
                    InitializeHourlyCounts();
                }

                // Optionally clear database counts (be careful with this)
                // _context.VehicleCounts.RemoveRange(_context.VehicleCounts);
                // await _context.SaveChangesAsync();

                _logger.LogInformation("Vehicle counts reset");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting vehicle counts");
            }
        }

        private void InitializeHourlyCounts()
        {
            var currentHour = DateTime.Now.ToString("yyyyMMddHH");
            _hourlyCounts.TryAdd(currentHour, 0);
        }
    }
}
