using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;
using OperaIQ.Domain.Entities;
using OperaIQ.Domain.Enums;

namespace OperaIQ.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly ITenantRepository<Project> _projectRepo;
        private readonly IMapper _mapper;

        public ProjectService(ITenantRepository<Project> projectRepo, IMapper mapper)
        {
            _projectRepo = projectRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ProjectDto>> GetAllProjectsAsync()
        {
            var projects = await _projectRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ProjectDto>>(projects);
        }

        public async Task<ProjectDto?> GetProjectByIdAsync(Guid id)
        {
            var project = await _projectRepo.GetByIdAsync(id);
            if (project == null) return null;
            return _mapper.Map<ProjectDto>(project);
        }

        public async Task<Result<Guid>> CreateProjectAsync(CreateProjectDto dto)
        {
            var project = _mapper.Map<Project>(dto);
            project.Status = ProjectStatus.Active;

            await _projectRepo.AddAsync(project);
            await _projectRepo.SaveChangesAsync();

            return Result.Success(project.Id);
        }

        public async Task<Result> CompleteProjectAsync(Guid id)
        {
            var project = await _projectRepo.GetByIdAsync(id);
            if (project == null) return Result.Failure("Không tìm thấy dự án.");

            project.Status = ProjectStatus.Completed;
            project.DueDate = DateTime.UtcNow; // cập nhật lại ngày hoàn thành thực tế

            _projectRepo.Update(project);
            await _projectRepo.SaveChangesAsync();

            return Result.Success();
        }
    }
}
