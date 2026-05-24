using System;

namespace OperaIQ.Application.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }
        public Guid UserId { get; set; }
        public string Type { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
