using Microsoft.AspNetCore.SignalR;

namespace ServerAPIApp.Hubs
{
    public class ResultHub : Hub
    {
        public async Task JoinGroupAsync(string requestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, requestId);
        }
    }
}
