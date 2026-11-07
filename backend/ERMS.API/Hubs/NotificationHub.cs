using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ERMS.API.Hubs;

/// <summary>
/// Authenticated SignalR Hub providing real-time notification streams.
/// Only authenticated users may establish websocket / long-polling connections.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user != null)
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = user.FindFirstValue(ClaimTypes.Role);
            var email = user.FindFirstValue(ClaimTypes.Email);

            if (!string.IsNullOrWhiteSpace(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Email_{email.ToLower()}");
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Role_{role}");
            }

            _logger.LogInformation("SignalR client connected: {ConnectionId} (User: {User}, Role: {Role})",
                Context.ConnectionId, email ?? userId, role);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
