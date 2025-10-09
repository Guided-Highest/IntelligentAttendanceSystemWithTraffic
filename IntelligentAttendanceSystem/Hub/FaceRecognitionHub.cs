namespace IntelligentAttendanceSystem.Hub
{
    // Hubs/FaceRecognitionHub.cs
    using Microsoft.AspNetCore.SignalR;
    public class FaceRecognitionHub : Hub
    {
        private readonly ILogger<FaceRecognitionHub> _logger;
        private static readonly HashSet<string> ConnectedClients = new HashSet<string>();
        private static readonly HashSet<string> TrafficMonitoringClients = new HashSet<string>();

        public FaceRecognitionHub(ILogger<FaceRecognitionHub> logger)
        {
            _logger = logger;
        }

        // Face Recognition Group Methods
        public async Task JoinFaceRecognitionGroup()
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "FaceRecognition");
                await Groups.AddToGroupAsync(Context.ConnectionId, "TrafficMonitoring");
                ConnectedClients.Add(Context.ConnectionId);
                _logger.LogInformation($"Client {Context.ConnectionId} joined FaceRecognition group. Total clients: {ConnectedClients.Count}");

                // Send welcome message
                await Clients.Caller.SendAsync("ConnectionStatus", new
                {
                    Status = "Connected",
                    Message = "Real-time face recognition connected successfully",
                    ConnectionId = Context.ConnectionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding client {Context.ConnectionId} to FaceRecognition group");
            }
        }

        public async Task LeaveFaceRecognitionGroup()
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "FaceRecognition");
                await Groups.AddToGroupAsync(Context.ConnectionId, "TrafficMonitoring");
                ConnectedClients.Remove(Context.ConnectionId);
                _logger.LogInformation($"Client {Context.ConnectionId} left FaceRecognition group. Total clients: {ConnectedClients.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing client {Context.ConnectionId} from FaceRecognition group");
            }
        }

        // Traffic Monitoring Group Methods
        public async Task JoinTrafficMonitoringGroup()
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "TrafficMonitoring");
                TrafficMonitoringClients.Add(Context.ConnectionId);
                _logger.LogInformation($"Client {Context.ConnectionId} joined TrafficMonitoring group. Total clients: {TrafficMonitoringClients.Count}");

                // Send welcome message
                await Clients.Caller.SendAsync("TrafficConnectionStatus", new
                {
                    Status = "Connected",
                    Message = "Traffic monitoring connected successfully",
                    ConnectionId = Context.ConnectionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding client {Context.ConnectionId} to TrafficMonitoring group");
            }
        }

        public async Task LeaveTrafficMonitoringGroup()
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "TrafficMonitoring");
                TrafficMonitoringClients.Remove(Context.ConnectionId);
                _logger.LogInformation($"Client {Context.ConnectionId} left TrafficMonitoring group. Total clients: {TrafficMonitoringClients.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing client {Context.ConnectionId} from TrafficMonitoring group");
            }
        }

        // Ping methods for both groups
        public async Task<string> Ping()
        {
            return await Task.FromResult($"Pong - {DateTime.Now:HH:mm:ss}");
        }

        public async Task<string> TrafficPing()
        {
            return await Task.FromResult($"Traffic Pong - {DateTime.Now:HH:mm:ss}");
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            ConnectedClients.Remove(Context.ConnectionId);
            TrafficMonitoringClients.Remove(Context.ConnectionId);

            if (exception != null)
            {
                _logger.LogWarning($"Client {Context.ConnectionId} disconnected with error: {exception.Message}");
            }
            else
            {
                _logger.LogInformation($"Client {Context.ConnectionId} disconnected gracefully");
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static int GetConnectedClientsCount()
        {
            return ConnectedClients.Count;
        }

        public static int GetTrafficMonitoringClientsCount()
        {
            return TrafficMonitoringClients.Count;
        }
    }
}
