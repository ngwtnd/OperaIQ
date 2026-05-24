using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaIQ.Application.DTOs;
using OperaIQ.Application.Services;
using OperaIQ.Domain.Entities;
using OperaIQ.Domain.Enums;
using OperaIQ.Infrastructure.Data;

namespace OperaIQ.Web.Controllers
{
    [Authorize(Policy = "TenantMember")]
    public class ProjectController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ProjectController(
            IProjectService projectService,
            UserManager<AppUser> userManager,
            ApplicationDbContext context)
        {
            _projectService = projectService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var projects = await _projectService.GetAllProjectsAsync();
            return View(projects);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var project = await _projectService.GetProjectByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            // Lấy danh sách task của dự án
            var tasks = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Where(t => t.ProjectId == id && !t.IsDeleted)
                .ToListAsync();

            // Lấy danh sách thành viên dự án
            var members = await _context.ProjectMembers
                .Include(pm => pm.User)
                .Where(pm => pm.ProjectId == id)
                .ToListAsync();

            ViewBag.Tasks = tasks;
            ViewBag.Members = members;

            // Nạp danh sách nhân viên khả dụng để hỗ trợ thêm thành viên mới
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var tenantId))
            {
                ViewBag.AllEmployees = await _userManager.Users
                    .Where(u => u.TenantId == tenantId && !u.IsDeleted)
                    .ToListAsync();
            }

            return View(project);
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Kiểm tra phân quyền: Chỉ Owner hoặc Admin được tạo dự án
            if (!User.IsInRole(nameof(UserSystemRole.TenantOwner)) && !User.IsInRole(nameof(UserSystemRole.TenantAdmin)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProjectDto dto)
        {
            if (!User.IsInRole(nameof(UserSystemRole.TenantOwner)) && !User.IsInRole(nameof(UserSystemRole.TenantAdmin)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            dto.CreatedById = userId;

            var result = await _projectService.CreateProjectAsync(dto);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                return View(dto);
            }

            // Tự động gán người tạo làm Project Manager trong bảng ProjectMembers
            var pm = new ProjectMember
            {
                ProjectId = result.Value,
                UserId = userId,
                Role = "Manager"
            };
            _context.ProjectMembers.Add(pm);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Dự án đã được khởi tạo thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(Guid projectId, Guid userId, string role)
        {
            if (!User.IsInRole(nameof(UserSystemRole.TenantOwner)) && !User.IsInRole(nameof(UserSystemRole.TenantAdmin)))
            {
                return RedirectToAction("AccessDenied", "Account");
            }

            var exists = await _context.ProjectMembers.AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId);
            if (!exists)
            {
                var member = new ProjectMember
                {
                    ProjectId = projectId,
                    UserId = userId,
                    Role = role
                };
                _context.ProjectMembers.Add(member);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm thành viên vào dự án.";
            }
            else
            {
                TempData["Error"] = "Thành viên này đã tham gia dự án.";
            }

            return RedirectToAction(nameof(Details), new { id = projectId });
        }
    }
}
