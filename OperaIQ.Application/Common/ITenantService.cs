using System;

namespace OperaIQ.Application.Common
{
    public interface ITenantService
    {
        Guid? TenantId { get; }
        string? TenantCode { get; }
        void SetTenant(Guid tenantId, string tenantCode);
    }
}
