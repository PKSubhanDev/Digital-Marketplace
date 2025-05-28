using Microsoft.AspNetCore.SignalR;

namespace UoNMarketPlace
{
    public class ChatHub: Hub
    {
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            // Send message to the specified receiver
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
        }

        public override Task OnConnectedAsync()
        {
            // Add the user to a group by their user ID
            var userId = Context.UserIdentifier;
            Groups.AddToGroupAsync(Context.ConnectionId, userId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            // Remove the user from the group on disconnect
            var userId = Context.UserIdentifier;
            Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
