namespace ProjectService;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

public class SignalingHub(ProjectDbContext context, ILogger<SignalingHub> logger) : Hub
{
    private readonly ProjectDbContext _context = context;
    private ILogger<SignalingHub> _logger = logger;
    
    public static Dictionary<string, List<string>> ConnectedClients = [];
    
     public async Task SendMessage(object message, string roomName) {
            await EmitLog("Client " + Context.ConnectionId + " said: " + message, roomName);

            await Clients.OthersInGroup(roomName).SendAsync("message", message);
        }

        public async Task JoinRoom(string roomName) {
            await EmitLog("Received request to create or join room " + roomName + " from client " + Context.ConnectionId, roomName);

            if (!ConnectedClients.ContainsKey(roomName)) {
                ConnectedClients.Add(roomName, new List<string>());
            }

            if (!ConnectedClients[roomName].Contains(Context.ConnectionId))
            {
                var project = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == roomName);

                project.Collaborators.Add(Context.ConnectionId);

                _context.Projects.Update(project);
                await _context.SaveChangesAsync();
                ConnectedClients[roomName].Add(Context.ConnectionId);
            }

            await EmitJoinRoom(roomName);

            var numberOfClients = ConnectedClients[roomName].Count;

            if (numberOfClients == 1) {
                await EmitCreated();
                await EmitLog("Client " + Context.ConnectionId + " created room " + roomName, roomName);
            } else {
                await EmitJoined(roomName);
                await EmitLog("Client " + Context.ConnectionId + " joined room " + roomName, roomName);
            }

            await EmitLog("Room " + roomName + " now has " + numberOfClients + " client(s)", roomName);
        }

        public async Task LeaveRoom(string roomName) {
            await EmitLog("Received request to leave room " + roomName + " from client " + Context.ConnectionId, roomName);

            if (ConnectedClients.ContainsKey(roomName) && ConnectedClients[roomName].Contains(Context.ConnectionId)) {
                ConnectedClients[roomName].Remove(Context.ConnectionId);
                await EmitLog("Client " + Context.ConnectionId + " left room " + roomName, roomName);

                if (ConnectedClients[roomName].Count == 0) {
                    ConnectedClients.Remove(roomName);
                    await EmitLog("Room " + roomName + " is now empty - resetting its state", roomName);
                }
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }

        private async Task EmitJoinRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        }

        private async Task EmitCreated()
        {
            await Clients.Caller.SendAsync("created");
        }

        private async Task EmitJoined(string roomName)
        {
            await Clients.Group(roomName).SendAsync("joined");
        }

        private async Task EmitLog(string message, string roomName)
        {
            Console.WriteLine("[Server]: " + message);
            await Clients.Group(roomName).SendAsync("log", "[Server]: " + message);
        }
}