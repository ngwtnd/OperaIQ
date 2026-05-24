using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaIQ.Application.Services;
using OperaIQ.Infrastructure.Data;

namespace OperaIQ.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly ITaskService _taskService;
        private readonly IDocumentService _documentService;
        private readonly ApplicationDbContext _context;

        public DashboardController(
            IProjectService projectService,
            ITaskService taskService,
            IDocumentService documentService,
            ApplicationDbContext context)
        {
            _projectService = projectService;
            _taskService = taskService;
            _documentService = documentService;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Kiểm tra xem user có TenantId không (Nếu là SuperAdmin và không có TenantId thì chuyển sang trang quản trị Tenant)
            var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantIdClaim))
            {
                if (User.IsInRole("SuperAdmin"))
                {
                    return RedirectToAction("ManageTenants", "SuperAdmin");
                }
                return RedirectToAction("Login", "Account");
            }

            var tenantId = Guid.Parse(tenantIdClaim);

            // Thống kê nhanh
            var projects = await _projectService.GetAllProjectsAsync();
            var documents = await _documentService.GetAllDocumentsAsync();
            
            // Lấy danh sách task của người dùng hiện tại
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var myTasks = await _taskService.GetMyTasksAsync(userId);

            ViewBag.ProjectCount = projects.Count();
            ViewBag.DocumentCount = documents.Count();
            ViewBag.TaskCount = myTasks.Count();
            ViewBag.CompletedTaskCount = myTasks.Count(t => t.Status == Domain.Enums.TaskStatus.Done);
            
            // Lấy thêm 5 task mới nhất của toàn dự án
            var allTasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.AssignedTo)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(allTasks);
        }
    }
}
