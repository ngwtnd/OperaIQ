using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace OperaIQ.Domain.Entities
{
    public class AppUser : IdentityUser<Guid>
    {
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public Guid? TenantId { get; set; }
        public virtual Tenant? Tenant { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = [];
        
        // Soft delete & auditing fields
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Cần cho OrgChart đệ quy
        public Guid? DepartmentId { get; set; }
        public virtual Department? Department { get; set; }
    }
}
