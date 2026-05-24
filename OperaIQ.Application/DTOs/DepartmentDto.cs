using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OperaIQ.Application.DTOs
{
    public class DepartmentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        
        public Guid? ParentDepartmentId { get; set; }
        public string? ParentDepartmentName { get; set; }
        
        public List<DepartmentDto> Children { get; set; } = [];
        public int MemberCount { get; set; }
    }

    public class CreateDepartmentDto
    {
        [Required(ErrorMessage = "Tên phòng ban không được để trống")]
        [StringLength(100, ErrorMessage = "Tên phòng ban tối đa 100 ký tự")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentDepartmentId { get; set; }
    }
}
