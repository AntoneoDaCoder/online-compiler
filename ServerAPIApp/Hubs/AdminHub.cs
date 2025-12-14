//using Microsoft.AspNetCore.SignalR;

//namespace ServerAPIApp.Hubs
//{
//    public class AdminHub : Hub
//    {
//        private static readonly string AdminGroup = "Admins";

//        public override async Task OnConnectedAsync()
//        {
//            var user = Context.User;

//            if (user.IsInRole("Admin"))
//            {
//                await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
//            }

//            await base.OnConnectedAsync();
//        }

//        public override async Task OnDisconnectedAsync(Exception? exception)
//        {
//            var user = Context.User;

//            if (user.IsInRole("Admin"))
//            {
//                await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminGroup);
//            }

//            await base.OnDisconnectedAsync(exception);
//        }

//    }
//}
