using System;
using System.ComponentModel.DataAnnotations;
using OperaIQ.Domain.Enums;
using TaskStatus = OperaIQ.Domain.Enums.TaskStatus;

namespace OperaIQ.Application.DTOs
{
    public class TaskDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Todo;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public DateTime? DueDate { get; set; }
        
        public Guid ProjectId { get; set; }
        public string? ProjectName { get; set; }
        
        public Guid? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        
        public bool IsAiAssigned { get; set; } = false;
        public string? AiReason { get; set; }
    }

    public class CreateTaskDto
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? Description { get; set; }

        [Required]
        public Guid ProjectId { get; set; }

        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        public DateTime? DueDate { get; set; }

        public Guid? AssignedToId { get; set; }

        public bool UseAiAssignment { get; set; } = false;
    }

    public class UpdateTaskStatusDto
    {
        public Guid Id { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Todo;
    }
}
