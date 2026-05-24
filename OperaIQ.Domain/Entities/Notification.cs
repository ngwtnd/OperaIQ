using System;
using OperaIQ.Domain.Common;

namespace OperaIQ.Domain.Entities
{
    public class Notification : TenantBaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        
        public Guid UserId { get; set; }
        public virtual AppUser User { get; set; } = null!;
        
        public string Type { get; set; } = "System"; // TaskAssigned, ProjectUpdate, System
    }
}
