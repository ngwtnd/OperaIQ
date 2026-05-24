using System;

namespace OperaIQ.Application.DTOs
{
    public class AiAssignmentDto
    {
        public Guid AssigneeId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
