using System;
using OperaIQ.Domain.Entities;

namespace OperaIQ.Domain.Common
{
    public abstract class TenantBaseEntity : BaseEntity
    {
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; } = null!;
    }
}
