using Microsoft.AspNetCore.SignalR;

namespace Library.Api.Hubs;

public sealed class NotificationHub : Hub
{
    public const string HubUrl = "/hubs/notifications";
    public const string ReceiveNotificationMethod = "ReceiveNotification";
    public const string ReceiveBorrowRequestMethod = "ReceiveBorrowRequest";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Librarian") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Librarians");
        }
        await base.OnConnectedAsync();
    }
}

