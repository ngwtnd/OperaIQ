using System;
using System.ComponentModel.DataAnnotations;
using OperaIQ.Domain.Enums;

namespace OperaIQ.Application.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public ProjectStatus Status { get; set; } = ProjectStatus.Active;
        
        public Guid CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        
        public int TaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
    }

    public class CreateProjectDto
    {
        [Required(ErrorMessage = "Tên dự án không được để trống")]
        [StringLength(200, ErrorMessage = "Tên dự án tối đa 200 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; } = DateTime.Today;
        public DateTime? DueDate { get; set; }
        public Guid CreatedById { get; set; }
    }
}
