using System.Linq;
using AutoMapper;
using OperaIQ.Application.DTOs;
using OperaIQ.Domain.Entities;

namespace OperaIQ.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // AppUser Mapping
            CreateMap<AppUser, UserDto>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null));

            // Project Mapping
            CreateMap<Project, ProjectDto>()
                .ForMember(dest => dest.CreatedByName, opt => opt.MapFrom(src => src.CreatedBy != null ? src.CreatedBy.FullName : null))
                .ForMember(dest => dest.TaskCount, opt => opt.MapFrom(src => src.Tasks != null ? src.Tasks.Count : 0))
                .ForMember(dest => dest.CompletedTaskCount, opt => opt.MapFrom(src => src.Tasks != null ? src.Tasks.Count(t => t.Status == Domain.Enums.TaskStatus.Done) : 0));
            CreateMap<CreateProjectDto, Project>();

            // ProjectTask Mapping
            CreateMap<ProjectTask, TaskDto>()
                .ForMember(dest => dest.ProjectName, opt => opt.MapFrom(src => src.Project != null ? src.Project.Name : null))
                .ForMember(dest => dest.AssignedToName, opt => opt.MapFrom(src => src.AssignedTo != null ? src.AssignedTo.FullName : null));
            CreateMap<CreateTaskDto, ProjectTask>()
                .ForMember(dest => dest.AssignedToId, opt => opt.MapFrom(src => src.AssignedToId));

            // Document Mapping
            CreateMap<Document, DocumentDto>()
                .ForMember(dest => dest.UploadedByName, opt => opt.MapFrom(src => src.UploadedBy != null ? src.UploadedBy.FullName : null));
            CreateMap<UploadDocumentDto, Document>();

            // Department Mapping
            CreateMap<Department, DepartmentDto>()
                .ForMember(dest => dest.ParentDepartmentName, opt => opt.MapFrom(src => src.ParentDepartment != null ? src.ParentDepartment.Name : null))
                .ForMember(dest => dest.MemberCount, opt => opt.MapFrom(src => src.Members != null ? src.Members.Count : 0));
            CreateMap<CreateDepartmentDto, Department>();

            // Notification Mapping
            CreateMap<Notification, NotificationDto>();
        }
    }
}
