using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OperaIQ.Application.DTOs;

namespace OperaIQ.Application.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId);
        Task SendNotificationAsync(Guid userId, string title, string message, string type);
        Task MarkAsReadAsync(Guid notificationId);
    }
}
