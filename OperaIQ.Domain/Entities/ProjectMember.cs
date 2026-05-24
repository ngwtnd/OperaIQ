using System;
using OperaIQ.Domain.Common;

namespace OperaIQ.Domain.Entities
{
    public class ProjectMember : TenantBaseEntity
    {
        public Guid ProjectId { get; set; }
        public virtual Project Project { get; set; } = null!;
        
        public Guid UserId { get; set; }
        public virtual AppUser User { get; set; } = null!;
        
        public string Role { get; set; } = string.Empty; // Ví dụ: Manager, Member
    }
}
