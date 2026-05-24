using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;

namespace OperaIQ.Application.Services
{
    public interface IAiTaskService
    {
        Task<Result<AiAssignmentDto>> SuggestAssigneeAsync(
            CreateTaskDto taskDto,
            IEnumerable<EmployeeProfileDto> availableEmployees,
            CancellationToken ct = default);

        Task<Result<string>> SummarizeDocumentAsync(
            string documentContent,
            CancellationToken ct = default);
    }
}
