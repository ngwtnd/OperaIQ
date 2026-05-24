using System;
using System.Collections.Generic;
using OperaIQ.Domain.Common;

namespace OperaIQ.Domain.Entities
{
    public class Department : TenantBaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentDepartmentId { get; set; }             // để vẽ org chart đệ quy
        public virtual Department? ParentDepartment { get; set; }
        public virtual ICollection<Department> Children { get; set; } = [];
        public virtual ICollection<AppUser> Members { get; set; } = [];
    }
}
