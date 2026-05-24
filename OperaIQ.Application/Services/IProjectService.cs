using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;

namespace OperaIQ.Application.Services
{
    public interface IProjectService
    {
        Task<IEnumerable<ProjectDto>> GetAllProjectsAsync();
        Task<ProjectDto?> GetProjectByIdAsync(Guid id);
        Task<Result<Guid>> CreateProjectAsync(CreateProjectDto dto);
        Task<Result> CompleteProjectAsync(Guid id);
    }
}
