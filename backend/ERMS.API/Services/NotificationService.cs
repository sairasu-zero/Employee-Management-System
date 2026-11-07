using ERMS.API.DTOs;
using ERMS.API.Hubs;
using ERMS.API.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ERMS.API.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendToUserAsync(Guid userId, NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notification to user {UserId}", userId);
        }
    }

    public async Task SendToEmailAsync(string email, NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"Email_{email.ToLower()}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notification to email {Email}", email);
        }
    }

    public async Task SendToRoleAsync(string roleName, NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"Role_{roleName}").SendAsync("ReceiveNotification", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SignalR notification to role {Role}", roleName);
        }
    }

    public async Task SendToRolesAsync(IEnumerable<string> roleNames, NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        foreach (var role in roleNames)
        {
            await SendToRoleAsync(role, notification, cancellationToken);
        }
    }

    public async Task BroadcastAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast SignalR notification");
        }
    }
}
