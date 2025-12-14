//using Microsoft.AspNetCore.SignalR;

//namespace ServerAPIApp.Hubs
//{
//    public class UserHub : Hub
//    {
//        // Метод для пользователей чтобы они могли получать персональные уведомления
//        public async Task SubscribeToUserEvents()
//        {
//            var userId = Context.UserIdentifier;
//            if (!string.IsNullOrEmpty(userId))
//            {
//                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
//            }
//        }

//        public override async Task OnDisconnectedAsync(Exception? exception)
//        {
//            var userId = Context.UserIdentifier;

//            if (!string.IsNullOrEmpty(userId))
//            {
//                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
//            }

//            await base.OnDisconnectedAsync(exception);
//        }
//    }
//}