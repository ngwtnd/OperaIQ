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
using TaskStatus = OperaIQ.Domain.Enums.TaskStatus;

namespace OperaIQ.Web.Controllers
{
    [Authorize(Policy = "TenantMember")]
    public class TaskController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IProjectService _projectService;
        private readonly UserManager<AppUser> _userManager;
        private readonly ApplicationDbContext _context;

        public TaskController(
            ITaskService taskService,
            IProjectService projectService,
            UserManager<AppUser> userManager,
            ApplicationDbContext context)
        {
            _taskService = taskService;
            _projectService = projectService;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = Guid.Parse(_userManager.GetUserId(User)!);
            var myTasks = await _taskService.GetMyTasksAsync(userId);
            return View(myTasks);
        }

        public async Task<IActionResult> Board(Guid projectId)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            var tasks = await _taskService.GetTasksByProjectAsync(projectId);
            ViewBag.Project = project;
            return View(tasks);
        }

        [HttpGet]
        [Authorize(Policy = "CanCreateTask")]
        public async Task<IActionResult> Create(Guid projectId)
        {
            var project = await _projectService.GetProjectByIdAsync(projectId);
            if (project == null) return NotFound();

            var tenantId = Guid.Parse(User.FindFirst("tenant_id")!.Value);
            var employees = await _userManager.Users
                .Where(u => u.TenantId == tenantId && !u.IsDeleted)
                .ToListAsync();

            ViewBag.Project = project;
            ViewBag.Employees = employees;
            
            var model = new CreateTaskDto { ProjectId = projectId };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanCreateTask")]
        public async Task<IActionResult> Create(CreateTaskDto dto)
        {
            var tenantId = Guid.Parse(User.FindFirst("tenant_id")!.Value);
            if (!ModelState.IsValid)
            {
                var project = await _projectService.GetProjectByIdAsync(dto.ProjectId);
                var employees = await _userManager.Users
                    .Where(u => u.TenantId == tenantId && !u.IsDeleted)
                    .ToListAsync();

                ViewBag.Project = project;
                ViewBag.Employees = employees;
                return View(dto);
            }

            var result = await _taskService.CreateTaskAsync(dto);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError(string.Empty, result.Error!);
                
                var project = await _projectService.GetProjectByIdAsync(dto.ProjectId);
                var employees = await _userManager.Users
                    .Where(u => u.TenantId == tenantId && !u.IsDeleted)
                    .ToListAsync();

                ViewBag.Project = project;
                ViewBag.Employees = employees;
                return View(dto);
            }

            TempData["Success"] = "Công việc mới đã được tạo thành công!";
            return RedirectToAction(nameof(Board), new { projectId = dto.ProjectId });
        }

        [HttpPost]
        [Authorize(Policy = "CanAssignTask")]
        public async Task<IActionResult> SuggestAssignee(Guid taskId)
        {
            var result = await _taskService.AutoAssignTaskWithAiAsync(taskId);
            if (result.IsSuccess)
            {
                var task = await _context.Tasks.Include(t => t.AssignedTo).FirstOrDefaultAsync(t => t.Id == taskId);
                return Json(new { success = true, assigneeName = task?.AssignedTo?.FullName, reason = task?.AiReason });
            }
            return Json(new { success = false, error = result.Error });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(Guid id, TaskStatus status)
        {
            var result = await _taskService.UpdateTaskStatusAsync(id, status);
            if (result.IsSuccess)
            {
                return Json(new { success = true });
            }
            return Json(new { success = false, error = result.Error });
        }
    }
}
