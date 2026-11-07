using ERMS.API.DTOs;

namespace ERMS.API.Interfaces;

public interface INotificationService
{
    Task SendToUserAsync(Guid userId, NotificationMessage notification, CancellationToken cancellationToken = default);
    Task SendToEmailAsync(string email, NotificationMessage notification, CancellationToken cancellationToken = default);
    Task SendToRoleAsync(string roleName, NotificationMessage notification, CancellationToken cancellationToken = default);
    Task SendToRolesAsync(IEnumerable<string> roleNames, NotificationMessage notification, CancellationToken cancellationToken = default);
    Task BroadcastAsync(NotificationMessage notification, CancellationToken cancellationToken = default);
}
