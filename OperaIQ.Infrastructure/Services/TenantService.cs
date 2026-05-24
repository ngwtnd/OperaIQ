using System;
using OperaIQ.Application.Common;

namespace OperaIQ.Infrastructure.Services
{
    public class TenantService : ITenantService
    {
        private Guid? _tenantId;
        private string? _tenantCode;

        public Guid? TenantId => _tenantId;
        public string? TenantCode => _tenantCode;

        public void SetTenant(Guid tenantId, string tenantCode)
        {
            _tenantId = tenantId;
            _tenantCode = tenantCode;
        }
    }
}
