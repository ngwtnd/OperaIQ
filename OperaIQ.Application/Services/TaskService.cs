using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;
using OperaIQ.Domain.Entities;
using OperaIQ.Domain.Enums;
using TaskStatus = OperaIQ.Domain.Enums.TaskStatus;

namespace OperaIQ.Application.Services
{
    public class TaskService : ITaskService
    {
        private readonly ITenantRepository<ProjectTask> _taskRepo;
        private readonly ITenantRepository<Project> _projectRepo;
        private readonly IAiTaskService _aiTaskService;
        private readonly INotificationService _notificationService;
        private readonly ITenantService _tenantService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMapper _mapper;

        public TaskService(
            ITenantRepository<ProjectTask> taskRepo,
            ITenantRepository<Project> projectRepo,
            IAiTaskService aiTaskService,
            INotificationService notificationService,
            ITenantService tenantService,
            UserManager<AppUser> userManager,
            IMapper mapper)
        {
            _taskRepo = taskRepo;
            _projectRepo = projectRepo;
            _aiTaskService = aiTaskService;
            _notificationService = notificationService;
            _tenantService = tenantService;
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TaskDto>> GetTasksByProjectAsync(Guid projectId)
        {
            var tasks = await _taskRepo.FindAsync(t => t.ProjectId == projectId);
            return _mapper.Map<IEnumerable<TaskDto>>(tasks);
        }

        public async Task<IEnumerable<TaskDto>> GetMyTasksAsync(Guid userId)
        {
            var tasks = await _taskRepo.FindAsync(t => t.AssignedToId == userId);
            return _mapper.Map<IEnumerable<TaskDto>>(tasks);
        }

        public async Task<TaskDto?> GetTaskByIdAsync(Guid id)
        {
            var task = await _taskRepo.GetByIdAsync(id);
            if (task == null) return null;
            return _mapper.Map<TaskDto>(task);
        }

        public async Task<Result<Guid>> CreateTaskAsync(CreateTaskDto dto)
        {
            var project = await _projectRepo.GetByIdAsync(dto.ProjectId);
            if (project == null) return Result.Failure<Guid>("Không tìm thấy dự án.");

            var task = _mapper.Map<ProjectTask>(dto);
            task.Status = TaskStatus.Todo;

            await _taskRepo.AddAsync(task);
            await _taskRepo.SaveChangesAsync();

            // Nếu sử dụng AI tự động phân công khi tạo
            if (dto.UseAiAssignment)
            {
                var assignResult = await AutoAssignTaskWithAiAsync(task.Id);
                if (!assignResult.IsSuccess)
                {
                    // Vẫn tạo task thành công, chỉ log lỗi phân công AI
                    // Có thể gán thủ công hoặc để trống
                }
            }
            else if (task.AssignedToId.HasValue)
            {
                // Nếu được gán thủ công
                await _notificationService.SendNotificationAsync(
                    task.AssignedToId.Value,
                    "Bạn được giao công việc mới",
                    $"Bạn đã được giao thực hiện công việc: '{task.Title}' trong dự án '{project.Name}'.",
                    "TaskAssigned"
                );
            }

            return Result.Success(task.Id);
        }

        public async Task<Result> UpdateTaskStatusAsync(Guid id, TaskStatus status)
        {
            var task = await _taskRepo.GetByIdAsync(id);
            if (task == null) return Result.Failure("Không tìm thấy công việc.");

            TaskStatus oldStatus = task.Status;
            task.Status = status;

            _taskRepo.Update(task);
            await _taskRepo.SaveChangesAsync();

            // Gửi thông báo
            var project = await _projectRepo.GetByIdAsync(task.ProjectId);
            if (project != null && task.AssignedToId.HasValue)
            {
                await _notificationService.SendNotificationAsync(
                    project.CreatedById, // Gửi cho quản lý dự án / người tạo dự án
                    "Cập nhật trạng thái công việc",
                    $"Công việc '{task.Title}' đã thay đổi trạng thái từ '{oldStatus}' sang '{status}'.",
                    "ProjectUpdate"
                );
            }

            return Result.Success();
        }

        public async Task<Result> AutoAssignTaskWithAiAsync(Guid taskId)
        {
            var task = await _taskRepo.GetByIdAsync(taskId);
            if (task == null) return Result.Failure("Không tìm thấy công việc.");

            Guid tenantId = _tenantService.TenantId ?? Guid.Empty;
            if (tenantId == Guid.Empty) return Result.Failure("Không thể xác định Context Tenant.");

            // Lấy danh sách nhân viên thuộc tenant khả dụng
            var employees = _userManager.Users
                .Where(u => u.TenantId == tenantId && !u.IsDeleted)
                .Select(u => new EmployeeProfileDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email ?? string.Empty,
                    Skills = u.FullName.Contains("Developer") ? "Lập trình phần mềm, ASP.NET Core" : "Kế toán, Quản trị, Marketing"
                })
                .ToList();

            if (!employees.Any())
            {
                return Result.Failure("Không có nhân viên khả dụng trong Tenant này.");
            }

            var taskDto = new CreateTaskDto
            {
                Title = task.Title,
                Description = task.Description,
                Priority = task.Priority,
                DueDate = task.DueDate,
                ProjectId = task.ProjectId
            };

            var aiResult = await _aiTaskService.SuggestAssigneeAsync(taskDto, employees);
            if (!aiResult.IsSuccess || aiResult.Value == null)
            {
                return Result.Failure($"Không thể gợi ý phân công từ AI: {aiResult.Error}");
            }

            task.AssignedToId = aiResult.Value.AssigneeId;
            task.IsAiAssigned = true;
            task.AiReason = aiResult.Value.Reason;

            _taskRepo.Update(task);
            await _taskRepo.SaveChangesAsync();

            // Gửi thông báo đến nhân viên được gán
            var project = await _projectRepo.GetByIdAsync(task.ProjectId);
            string projectName = project?.Name ?? "Dự án chung";

            await _notificationService.SendNotificationAsync(
                task.AssignedToId.Value,
                "Tự động phân công công việc bằng AI",
                $"AI đã tự động phân công cho bạn thực hiện công việc '{task.Title}' trong dự án '{projectName}' với lý do: {task.AiReason}",
                "TaskAssigned"
            );

            return Result.Success();
        }
    }
}
