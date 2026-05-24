using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;

namespace OperaIQ.Application.Services
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetOrgChartAsync();
        Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync();
        Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id);
        Task<Result<Guid>> CreateDepartmentAsync(CreateDepartmentDto dto);
    }
}
