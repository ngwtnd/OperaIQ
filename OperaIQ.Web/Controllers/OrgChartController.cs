using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaIQ.Application.DTOs;
using OperaIQ.Application.Services;
using OperaIQ.Domain.Enums;
using OperaIQ.Infrastructure.Data;

namespace OperaIQ.Web.Controllers
{
    [Authorize(Policy = "TenantMember")]
    public class OrgChartController : Controller
    {
        private readonly IDepartmentService _deptService;
        private readonly ApplicationDbContext _context;

        public OrgChartController(IDepartmentService deptService, ApplicationDbContext context)
        {
            _deptService = deptService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var chart = await _deptService.GetOrgChartAsync();
            return View(chart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(CreateDepartmentDto dto)
        {
            if (!User.IsInRole(nameof(UserSystemRole.TenantOwner)))
            {
                TempData["Error"] = "Chỉ Quản trị doanh nghiệp (TenantOwner) mới có quyền tạo phòng ban.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu phòng ban không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _deptService.CreateDepartmentAsync(dto);
            if (result.IsSuccess)
            {
                TempData["Success"] = $"Đã khởi tạo phòng ban '{dto.Name}' thành công!";
            }
            else
            {
                TempData["Error"] = $"Không thể khởi tạo phòng ban: {result.Error}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
