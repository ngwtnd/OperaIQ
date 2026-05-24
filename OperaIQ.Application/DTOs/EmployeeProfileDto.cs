using System;

namespace OperaIQ.Application.DTOs
{
    public class EmployeeProfileDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Skills { get; set; }
    }
}
