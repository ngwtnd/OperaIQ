## Context (carry forward)
- Stack: ASP.NET Core MVC .NET 10, Entity Framework Core Code First, SQL Server
- Architecture: 4-layer (Web / Application / Domain / Infrastructure)
- Multi-tenant SaaS, RBAC with 4 roles: SuperAdmin, TenantOwner, TenantAdmin, Employee
- AI integration: Claude API — auto task assignment + document summarization
- Real-time: SignalR for notifications
- Rules file: workflow-ai-rules.md already established — MUST follow it at all times

## Objective
Scaffold the complete WorkFlow AI system from zero to a running application matching the attached workflow diagram.

## Starting State
Empty solution. .NET 10 SDK installed. SQL Server available. No files exist yet.

## Target State
A fully runnable ASP.NET Core MVC solution with:
- All 4 projects created (Web, Application, Domain, Infrastructure)
- EF Core Code First entities and DbContext configured
- ASP.NET Core Identity + JWT authentication wired up
- RBAC policies matching the 4-role hierarchy
- Multi-tenant isolation enforced at repository level
- Controllers and Views for: Dashboard, Tasks, Projects, Documents, OrgChart, Notifications
- AI service interface stubbed with Claude API client
- SignalR hub for real-time notifications
- Initial migration generated and applied
- Seed data: 1 SuperAdmin, 1 demo Tenant with Owner + Admin + 2 Employees

## Allowed Actions
- Create and edit files inside the solution directory only
- Run `dotnet` CLI commands (new, add, ef migrations, ef database)
- Install NuGet packages via `dotnet add package`
- Read workflow-ai-rules.md before writing any entity or service

## Forbidden Actions
- Do NOT modify files outside the solution directory
- Do NOT run `dotnet run` or deploy
- Do NOT push to git
- Do NOT use int for primary keys — MUST use Guid
- Do NOT place business logic in Controllers
- Do NOT expose Entity classes directly to Views — always use DTOs
- Do NOT make architecture decisions that contradict workflow-ai-rules.md without stopping first

## Stop Conditions
Pause and ask for human review when:
- A design decision requires choosing between two valid architecture patterns
- An EF Core relationship is ambiguous (e.g. self-referencing Department tree)
- The AI service integration requires an actual API key to proceed
- A migration would drop or truncate an existing table
- Any task requires changes outside the 4-project solution structure

## Execution Order — follow this sequence exactly
1. Create solution and 4 projects, wire project references
2. Define all Domain entities (BaseEntity, TenantBaseEntity, then all core entities)
3. Configure DbContext with global query filters and SaveChangesAsync override
4. Register services in Program.cs (Identity, JWT, EF Core, RBAC policies, SignalR, ITenantService)
5. Implement TenantService and TenantRepository<T>
6. Scaffold Application layer: interfaces, DTOs, Result<T>, AutoMapper profiles
7. Implement services: TaskService, ProjectService, DocumentService, AiTaskService (stub)
8. Create Controllers (one per feature area) with correct [Authorize(Policy)] attributes
9. Create Razor Views with @model DTOs — no Entity types in Views
10. Add SignalR NotificationHub
11. Generate initial EF Core migration and seed data
12. Verify solution builds with zero errors before stopping

## Checkpoints
After each numbered step above, output: ✅ [step name — files created/modified]
At the end, output a full list of every file created with its relative path.

## Constraints
- Follow workflow-ai-rules.md naming conventions exactly
- Every async method MUST have the Async suffix and use await — no .Result or .Wait()
- Every Controller action POST MUST have [ValidateAntiForgeryToken]
- Every TenantBaseEntity query MUST filter by TenantId — no exceptions without a comment
- NuGet packages: Microsoft.EntityFrameworkCore.SqlServer, Microsoft.AspNetCore.Identity.EntityFrameworkCore, Microsoft.AspNetCore.Authentication.JwtBearer, AutoMapper.Extensions.Microsoft.DependencyInjection, Microsoft.AspNetCore.SignalR, Hangfire.AspNetCore, Serilog.AspNetCore