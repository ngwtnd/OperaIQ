using System;
using OperaIQ.Domain.Common;
using OperaIQ.Domain.Enums;
// using TaskStatus = OperaIQ.Domain.Enums.TaskStatus;

namespace OperaIQ.Domain.Entities
{
    public class ProjectTask : TenantBaseEntity
    {
        public Guid ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public OperaIQ.Domain.Enums.TaskStatus Status { get; set; }
    = OperaIQ.Domain.Enums.TaskStatus.Todo;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public Guid? AssignedToId { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsAiAssigned { get; set; } = false;           // AI giao hay người giao
        public string? AiReason { get; set; }                     // lý do AI chọn người này
        
        public virtual Project Project { get; set; } = null!;
        public virtual AppUser? AssignedTo { get; set; }
    }
}
