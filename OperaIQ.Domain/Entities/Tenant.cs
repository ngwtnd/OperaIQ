using System;
using System.Collections.Generic;
using OperaIQ.Domain.Common;
using OperaIQ.Domain.Enums;

namespace OperaIQ.Domain.Entities
{
    public class Tenant : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;         // unique, dùng làm subdomain
        public TenantStatus Status { get; set; } = TenantStatus.Active;
        public string? LogoUrl { get; set; }
        public DateTime? SubscriptionExpiry { get; set; }

        public virtual ICollection<AppUser> Users { get; set; } = [];
        public virtual ICollection<Department> Departments { get; set; } = [];
        public virtual ICollection<Project> Projects { get; set; } = [];
    }
}
