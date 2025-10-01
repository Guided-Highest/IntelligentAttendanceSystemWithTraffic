namespace IntelligentAttendanceSystem.Hub
{
    // Hubs/FaceRecognitionHub.cs
    using Microsoft.AspNetCore.SignalR;

    public class FaceRecognitionHub : Hub
    {
        private readonly ILogger<FaceRecognitionHub> _logger;
        private static readonly HashSet<string> ConnectedClients = new HashSet<string>();


        public FaceRecognitionHub(ILogger<FaceRecognitionHub> logger)
        {
            _logger = logger;
        }

        public async Task JoinFaceRecognitionGroup()
        {
            try
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "FaceRecognition");
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
                _logger.LogError(ex, $"Error adding client {Context.ConnectionId} to group");
            }
        }

        public async Task LeaveFaceRecognitionGroup()
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "FaceRecognition");
                ConnectedClients.Remove(Context.ConnectionId);
                _logger.LogInformation($"Client {Context.ConnectionId} left FaceRecognition group. Total clients: {ConnectedClients.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing client {Context.ConnectionId} from group");
            }
        }

        public async Task<string> Ping()
        {
            return await Task.FromResult($"Pong - {DateTime.Now:HH:mm:ss}");
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            ConnectedClients.Remove(Context.ConnectionId);

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
    }
}
