using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OperaIQ.Domain.Entities;
using OperaIQ.Domain.Enums;

namespace OperaIQ.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(
            ApplicationDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            // Đảm bảo database đã được tạo
            await context.Database.MigrateAsync();

            // 1. Seed Roles hệ thống
            string[] roleNames = { 
                nameof(UserSystemRole.SuperAdmin), 
                nameof(UserSystemRole.TenantOwner), 
                nameof(UserSystemRole.TenantAdmin), 
                nameof(UserSystemRole.Employee) 
            };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }

            // 2. Seed SuperAdmin (Không thuộc Tenant nào)
            string superAdminEmail = "superadmin@operaiq.vn";
            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdmin == null)
            {
                superAdmin = new AppUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FullName = "Hệ thống SuperAdmin",
                    EmailConfirmed = true,
                    TenantId = null // Không thuộc tenant nào
                };
                
                var result = await userManager.CreateAsync(superAdmin, "Password123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, nameof(UserSystemRole.SuperAdmin));
                }
            }

            // 3. Seed Tenants mẫu
            if (!await context.Tenants.IgnoreQueryFilters().AnyAsync())
            {
                var tenant1 = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = "Công ty Cổ phần Công nghệ OperaIQ",
                    Slug = "operaiq",
                    Status = TenantStatus.Active
                };

                var tenant2 = new Tenant
                {
                    Id = Guid.NewGuid(),
                    Name = "Tập đoàn Bán lẻ VinGroup",
                    Slug = "vingroup",
                    Status = TenantStatus.Active
                };

                await context.Tenants.AddRangeAsync(tenant1, tenant2);
                await context.SaveChangesAsync();

                // 4. Seed Phòng ban (Departments) cho Tenant 1 (OperaIQ)
                var deptTech = new Department
                {
                    Id = Guid.NewGuid(),
                    Name = "Phòng Công nghệ & Phát triển",
                    Description = "Nghiên cứu phát triển sản phẩm công nghệ",
                    TenantId = tenant1.Id
                };

                var deptHr = new Department
                {
                    Id = Guid.NewGuid(),
                    Name = "Phòng Nhân sự & Hành chính",
                    Description = "Quản lý nhân sự và các hoạt động hành chính",
                    TenantId = tenant1.Id
                };

                await context.Departments.AddRangeAsync(deptTech, deptHr);
                await context.SaveChangesAsync();

                // 5. Seed Users cho Tenant 1 (OperaIQ)
                var owner = new AppUser
                {
                    UserName = "owner@operaiq.vn",
                    Email = "owner@operaiq.vn",
                    FullName = "Nguyễn Văn Chủ",
                    EmailConfirmed = true,
                    TenantId = tenant1.Id,
                    DepartmentId = null
                };
                if ((await userManager.CreateAsync(owner, "Password123!")).Succeeded)
                {
                    await userManager.AddToRoleAsync(owner, nameof(UserSystemRole.TenantOwner));
                }

                var admin = new AppUser
                {
                    UserName = "admin@operaiq.vn",
                    Email = "admin@operaiq.vn",
                    FullName = "Trần Thị Quản Lý",
                    EmailConfirmed = true,
                    TenantId = tenant1.Id,
                    DepartmentId = null
                };
                if ((await userManager.CreateAsync(admin, "Password123!")).Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, nameof(UserSystemRole.TenantAdmin));
                }

                var dev = new AppUser
                {
                    UserName = "dev1@operaiq.vn",
                    Email = "dev1@operaiq.vn",
                    FullName = "Phạm Văn Lập Trình (Developer)",
                    EmailConfirmed = true,
                    TenantId = tenant1.Id,
                    DepartmentId = deptTech.Id
                };
                if ((await userManager.CreateAsync(dev, "Password123!")).Succeeded)
                {
                    await userManager.AddToRoleAsync(dev, nameof(UserSystemRole.Employee));
                }

                var hr = new AppUser
                {
                    UserName = "hr1@operaiq.vn",
                    Email = "hr1@operaiq.vn",
                    FullName = "Lê Thị Tuyển Dụng",
                    EmailConfirmed = true,
                    TenantId = tenant1.Id,
                    DepartmentId = deptHr.Id
                };
                if ((await userManager.CreateAsync(hr, "Password123!")).Succeeded)
                {
                    await userManager.AddToRoleAsync(hr, nameof(UserSystemRole.Employee));
                }

                // 6. Seed Dự án mẫu (Projects)
                var project = new Project
                {
                    Id = Guid.NewGuid(),
                    Name = "Phát triển Nền tảng OperaIQ WorkFlow AI",
                    Description = "Xây dựng nền tảng SaaS quản lý công việc thế hệ mới kết hợp Claude AI",
                    Status = ProjectStatus.Active,
                    CreatedById = owner.Id,
                    TenantId = tenant1.Id
                };
                await context.Projects.AddAsync(project);
                await context.SaveChangesAsync();

                // Đăng ký thành viên dự án
                var pm1 = new ProjectMember { ProjectId = project.Id, UserId = owner.Id, Role = "Manager", TenantId = tenant1.Id };
                var pm2 = new ProjectMember { ProjectId = project.Id, UserId = admin.Id, Role = "SubManager", TenantId = tenant1.Id };
                var pm3 = new ProjectMember { ProjectId = project.Id, UserId = dev.Id, Role = "Developer", TenantId = tenant1.Id };
                await context.ProjectMembers.AddRangeAsync(pm1, pm2, pm3);

                // 7. Seed Tasks mẫu
                var task1 = new ProjectTask
                {
                    ProjectId = project.Id,
                    Title = "Thiết kế cơ sở dữ liệu Multi-tenant và cấu hình EF Core",
                    Description = "Thiết lập cấu trúc CSDL cô lập tenant dựa trên Query Filters và Soft Delete.",
                    Status = Domain.Enums.TaskStatus.Done,
                    Priority = TaskPriority.High,
                    AssignedToId = dev.Id,
                    DueDate = DateTime.UtcNow.AddDays(-2),
                    IsAiAssigned = false,
                    TenantId = tenant1.Id
                };

                var task2 = new ProjectTask
                {
                    ProjectId = project.Id,
                    Title = "Tích hợp Claude API tự động đề xuất phân công nhân sự",
                    Description = "Viết Service tích hợp HttpClient gọi Claude API phân tích kỹ năng nhân viên và phân công task phù hợp.",
                    Status = Domain.Enums.TaskStatus.InProgress,
                    Priority = TaskPriority.High,
                    AssignedToId = dev.Id,
                    DueDate = DateTime.UtcNow.AddDays(5),
                    IsAiAssigned = true,
                    AiReason = "[AI Claude Gợi ý] Nhân viên Phạm Văn Lập Trình có kỹ năng 'Lập trình phần mềm, ASP.NET Core' hoàn toàn trùng khớp với yêu cầu kỹ thuật của công việc phát triển API này.",
                    TenantId = tenant1.Id
                };

                var task3 = new ProjectTask
                {
                    ProjectId = project.Id,
                    Title = "Thiết kế giao diện Kanban Board cao cấp",
                    Description = "Xây dựng giao diện kéo thả công việc trực quan, màu sắc hiện đại chuẩn UX.",
                    Status = Domain.Enums.TaskStatus.Todo,
                    Priority = TaskPriority.Medium,
                    AssignedToId = null,
                    DueDate = DateTime.UtcNow.AddDays(10),
                    IsAiAssigned = false,
                    TenantId = tenant1.Id
                };

                await context.Tasks.AddRangeAsync(task1, task2, task3);
                await context.SaveChangesAsync();
            }
        }
    }
}
