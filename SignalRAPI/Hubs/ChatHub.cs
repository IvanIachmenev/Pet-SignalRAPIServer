using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace SignalRAPI.Hubs
{
    public interface IChatClient
    {
        Task ReceiveMessage(string userName, string message);
    }

    public class ChatHub : Hub<IChatClient>
    {
        private readonly IDistributedCache _cache;
        public ChatHub(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task JoinChat(UserConnection connection)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, connection.ChatName);

            var stringConnection = JsonSerializer.Serialize(connection);

            await _cache.SetStringAsync(Context.ConnectionId, stringConnection);

            await Clients.Group(connection.ChatName).ReceiveMessage("Admin", $"{connection.UserName} join chat!");
        }

        public async Task SendMessage(string message)
        {
            var stringConnection = await _cache.GetAsync(Context.ConnectionId);

            var connection = JsonSerializer.Deserialize<UserConnection>(stringConnection);

            if(connection is not null)
            {
                await Clients.Group(connection.ChatName).ReceiveMessage("Admin", $"{message}");
            }

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var stringConnection = await _cache.GetAsync(Context.ConnectionId);

            var connection = JsonSerializer.Deserialize<UserConnection>(stringConnection);

            if(connection is not null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, connection.ChatName);

                await Clients.Group(connection.ChatName).ReceiveMessage("Admin", $"{connection.UserName} leave with POZOR");

                await _cache.RemoveAsync(Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
    public record UserConnection(string UserName, string ChatName);
}
