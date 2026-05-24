using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaIQ.Domain.Entities;
using OperaIQ.Domain.Enums;
using OperaIQ.Infrastructure.Data;

namespace OperaIQ.Web.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuperAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ManageTenants()
        {
            var tenants = await _context.Tenants.IgnoreQueryFilters().ToListAsync();
            return View(tenants);
        }

        [HttpGet]
        public IActionResult CreateTenant()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTenant(Tenant tenant)
        {
            if (ModelState.IsValid)
            {
                tenant.Id = Guid.NewGuid();
                tenant.CreatedAt = DateTime.UtcNow;
                tenant.UpdatedAt = DateTime.UtcNow;
                tenant.Status = TenantStatus.Active;
                tenant.Slug = tenant.Slug.ToLower().Trim();

                var exists = await _context.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == tenant.Slug);
                if (exists)
                {
                    ModelState.AddModelError("Slug", "Slug này đã được sử dụng bởi công ty khác.");
                    return View(tenant);
                }

                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã khởi tạo công ty {tenant.Name} thành công!";
                return RedirectToAction(nameof(ManageTenants));
            }

            return View(tenant);
        }
    }
}
