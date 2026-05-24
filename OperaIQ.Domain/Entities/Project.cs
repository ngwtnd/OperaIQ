using System;
using System.Collections.Generic;
using OperaIQ.Domain.Common;
using OperaIQ.Domain.Enums;

namespace OperaIQ.Domain.Entities
{
    public class Project : TenantBaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        
        public Guid CreatedById { get; set; }
        public virtual AppUser CreatedBy { get; set; } = null!;
        
        public virtual ICollection<ProjectTask> Tasks { get; set; } = [];
        public virtual ICollection<ProjectMember> Members { get; set; } = [];
    }
}
