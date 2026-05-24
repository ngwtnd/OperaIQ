using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OperaIQ.Application.Common;
using OperaIQ.Infrastructure.Data;

namespace OperaIQ.Web.Middlewares
{
    public class TenantResolutionMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantResolutionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantService tenantService, ApplicationDbContext dbContext)
        {
            Guid? tenantId = null;
            string? tenantSlug = null;

            // 1. Độ ưu tiên 1: Lấy TenantId từ Claims của User đã đăng nhập
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
                if (!string.IsNullOrEmpty(tenantIdClaim) && Guid.TryParse(tenantIdClaim, out var claimGuid))
                {
                    tenantId = claimGuid;
                    
                    // Lấy Tenant Slug từ claim nếu có, hoặc truy vấn nhanh từ DB
                    tenantSlug = context.User.FindFirst("tenant_slug")?.Value;
                    if (string.IsNullOrEmpty(tenantSlug) && tenantId.HasValue)
                    {
                        var tenant = await dbContext.Tenants.FindAsync(tenantId.Value);
                        tenantSlug = tenant?.Slug;
                    }
                }
            }

            // 2. Độ ưu tiên 2: Phân tích Subdomain làm phương án dự phòng (cho trang đăng nhập, công cộng, v.v.)
            if (tenantId == null)
            {
                var host = context.Request.Host.Value; // ví dụ: tenant1.operaiq.com hoặc localhost:5001
                var hostParts = host.Split('.');
                
                if (hostParts.Length > 2 && !hostParts[0].Equals("www", StringComparison.OrdinalIgnoreCase))
                {
                    string slug = hostParts[0];
                    // Truy vấn DB tìm Tenant theo Slug
                    var tenant = await dbContext.Tenants
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(t => t.Slug == slug && t.Status == Domain.Enums.TenantStatus.Active && !t.IsDeleted);

                    if (tenant != null)
                    {
                        tenantId = tenant.Id;
                        tenantSlug = tenant.Slug;
                    }
                }
            }

            // 3. Độ ưu tiên 3: Lấy từ Route hoặc Query (hỗ trợ môi trường phát triển localhost dễ dàng)
            if (tenantId == null)
            {
                string? slug = context.Request.Query["tenant"].FirstOrDefault();
                if (!string.IsNullOrEmpty(slug))
                {
                    var tenant = await dbContext.Tenants
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(t => t.Slug == slug && t.Status == Domain.Enums.TenantStatus.Active && !t.IsDeleted);

                    if (tenant != null)
                    {
                        tenantId = tenant.Id;
                        tenantSlug = tenant.Slug;
                    }
                }
            }

            // 4. Thiết lập Tenant context cho request hiện tại
            if (tenantId.HasValue && !string.IsNullOrEmpty(tenantSlug))
            {
                tenantService.SetTenant(tenantId.Value, tenantSlug);
                
                // Lưu vào HttpContext Items để dùng ở các lớp khác nếu cần
                context.Items["TenantId"] = tenantId.Value;
                context.Items["TenantSlug"] = tenantSlug;
            }

            await _next(context);
        }
    }
}
