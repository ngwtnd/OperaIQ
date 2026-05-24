using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;
using OperaIQ.Application.Services;
using OperaIQ.Domain.Entities;
using OperaIQ.Infrastructure.Hubs;

namespace OperaIQ.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ITenantRepository<Notification> _notificationRepo;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IMapper _mapper;

        public NotificationService(
            ITenantRepository<Notification> notificationRepo,
            IHubContext<NotificationHub> hubContext,
            IMapper mapper)
        {
            _notificationRepo = notificationRepo;
            _hubContext = hubContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId)
        {
            var notifications = await _notificationRepo.FindAsync(n => n.UserId == userId);
            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task SendNotificationAsync(Guid userId, string title, string message, string type)
        {
            // 1. Lưu thông báo vào cơ sở dữ liệu
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false
            };

            await _notificationRepo.AddAsync(notification);
            await _notificationRepo.SaveChangesAsync();

            // 2. Gửi thông báo real-time qua SignalR
            var dto = _mapper.Map<NotificationDto>(notification);
            
            // Gửi trực tiếp đến User cụ thể bằng userId
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", dto);
            
            // Gửi toàn bộ để cập nhật cho người dùng (nếu có trường hợp đặc biệt, ở đây gửi trực tiếp)
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var notification = await _notificationRepo.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                _notificationRepo.Update(notification);
                await _notificationRepo.SaveChangesAsync();
            }
        }
    }
}
