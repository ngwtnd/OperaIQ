using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;
using OperaIQ.Domain.Entities;

namespace OperaIQ.Application.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ITenantRepository<Department> _deptRepo;
        private readonly IMapper _mapper;

        public DepartmentService(ITenantRepository<Department> deptRepo, IMapper mapper)
        {
            _deptRepo = deptRepo;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync()
        {
            var depts = await _deptRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<DepartmentDto>>(depts);
        }

        public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id)
        {
            var dept = await _deptRepo.GetByIdAsync(id);
            if (dept == null) return null;
            return _mapper.Map<DepartmentDto>(dept);
        }

        public async Task<IEnumerable<DepartmentDto>> GetOrgChartAsync()
        {
            var allDepts = await _deptRepo.GetAllAsync();
            var dtos = _mapper.Map<IEnumerable<DepartmentDto>>(allDepts).ToList();

            // Xây dựng sơ đồ cây đệ quy từ bộ nhớ
            var lookup = dtos.ToDictionary(d => d.Id);
            var rootNodes = new List<DepartmentDto>();

            foreach (var dto in dtos)
            {
                if (dto.ParentDepartmentId.HasValue && lookup.ContainsKey(dto.ParentDepartmentId.Value))
                {
                    lookup[dto.ParentDepartmentId.Value].Children.Add(dto);
                }
                else
                {
                    rootNodes.Add(dto);
                }
            }

            return rootNodes;
        }

        public async Task<Result<Guid>> CreateDepartmentAsync(CreateDepartmentDto dto)
        {
            var dept = _mapper.Map<Department>(dto);

            if (dto.ParentDepartmentId.HasValue)
            {
                var parent = await _deptRepo.GetByIdAsync(dto.ParentDepartmentId.Value);
                if (parent == null)
                {
                    return Result.Failure<Guid>("Phòng ban cha không tồn tại.");
                }
            }

            await _deptRepo.AddAsync(dept);
            await _deptRepo.SaveChangesAsync();

            return Result.Success(dept.Id);
        }
    }
}
