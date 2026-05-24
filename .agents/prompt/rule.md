# AI Coding Rules — WorkFlow AI System
# Stack: ASP.NET Core MVC .NET 10 · Entity Framework Core (Code First) · SQL Server

---

## 1. Tổng quan dự án

Đây là hệ thống **WorkFlow AI** — nền tảng quản lý công việc đa tenant, tích hợp AI phân công task tự động và xử lý tài liệu.

**Tech stack bắt buộc:**
- ASP.NET Core MVC — .NET 10
- Entity Framework Core (Code First) — SQL Server
- Authentication: ASP.NET Core Identity + JWT Bearer
- AI Integration: Anthropic Claude API hoặc OpenAI API
- Real-time: SignalR
- Background Jobs: Hangfire hoặc .NET BackgroundService

---

## 2. Cấu trúc thư mục chuẩn

```
WorkFlowAI/
├── WorkFlowAI.Web/              # ASP.NET Core MVC project
│   ├── Controllers/
│   ├── Views/
│   ├── wwwroot/
│   └── Program.cs
├── WorkFlowAI.Application/      # Business logic, Services, DTOs
│   ├── Services/
│   ├── DTOs/
│   ├── Interfaces/
│   └── Mappings/
├── WorkFlowAI.Domain/           # Entities, Enums, Domain logic
│   ├── Entities/
│   ├── Enums/
│   └── Interfaces/
├── WorkFlowAI.Infrastructure/   # EF Core, Repositories, AI clients
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── Migrations/
│   ├── Repositories/
│   └── AI/
└── WorkFlowAI.Tests/
```

**Quy tắc:** Không được để business logic trong Controller. Controller chỉ gọi Service, trả về View hoặc JSON.

---

## 3. Entity & Database (Code First)

### 3.1 Quy tắc đặt tên Entity

- Entity class: **PascalCase**, singular — `Tenant`, `AppUser`, `ProjectTask`
- Table name: **PascalCase**, plural — cấu hình qua `modelBuilder.ToTable("Tasks")`
- Primary key: `Id` kiểu `Guid` — KHÔNG dùng `int` autoincrement cho entity chính
- Foreign key: `{EntityName}Id` — ví dụ `TenantId`, `AssignedToId`
- Timestamp: luôn có `CreatedAt`, `UpdatedAt` — kiểu `DateTime` (UTC)
- Soft delete: luôn có `IsDeleted` kiểu `bool`, `DeletedAt` kiểu `DateTime?`

### 3.2 Base Entity bắt buộc

```csharp
// Domain/Entities/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}

// Domain/Entities/TenantBaseEntity.cs — cho mọi entity thuộc tenant
public abstract class TenantBaseEntity : BaseEntity
{
    public Guid TenantId { get; set; }
    public virtual Tenant Tenant { get; set; } = null!;
}
```

### 3.3 Các Entity cốt lõi

```csharp
// Tenant — công ty / tổ chức
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;         // unique, dùng làm subdomain
    public TenantStatus Status { get; set; } = TenantStatus.Active;
    public string? LogoUrl { get; set; }
    public DateTime? SubscriptionExpiry { get; set; }

    public virtual ICollection<AppUser> Users { get; set; } = [];
    public virtual ICollection<Department> Departments { get; set; } = [];
    public virtual ICollection<Project> Projects { get; set; } = [];
}

// AppUser — mở rộng IdentityUser
public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public Guid? TenantId { get; set; }
    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    // Soft delete fields
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// Department — phòng ban trong tenant
public class Department : TenantBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentDepartmentId { get; set; }             // để vẽ org chart đệ quy
    public virtual Department? ParentDepartment { get; set; }
    public virtual ICollection<Department> Children { get; set; } = [];
    public virtual ICollection<AppUser> Members { get; set; } = [];
}

// Project
public class Project : TenantBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid CreatedById { get; set; }
    public virtual AppUser CreatedBy { get; set; } = null!;
    public virtual ICollection<ProjectTask> Tasks { get; set; } = [];
    public virtual ICollection<ProjectMember> Members { get; set; } = [];
}

// ProjectTask
public class ProjectTask : TenantBaseEntity
{
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public Guid? AssignedToId { get; set; }
    public DateTime? DueDate { get; set; }
    public bool IsAiAssigned { get; set; } = false;           // AI giao hay người giao
    public string? AiReason { get; set; }                     // lý do AI chọn người này
    public virtual Project Project { get; set; } = null!;
    public virtual AppUser? AssignedTo { get; set; }
}

// Document
public class Document : TenantBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? AiSummary { get; set; }                    // tóm tắt do AI tạo
    public Guid UploadedById { get; set; }
    public virtual AppUser UploadedBy { get; set; } = null!;
    public virtual ICollection<DocumentPermission> Permissions { get; set; } = [];
}

// DocumentPermission — ACL cho tài liệu
public class DocumentPermission : BaseEntity
{
    public Guid DocumentId { get; set; }
    public Guid? UserId { get; set; }                         // null = áp cho role
    public string? RoleName { get; set; }
    public PermissionLevel Level { get; set; }                // View, Download, Edit
    public virtual Document Document { get; set; } = null!;
}
```

### 3.4 Enums

```csharp
// Domain/Enums/
public enum TenantStatus    { Active, Suspended, Expired }
public enum ProjectStatus   { Active, Completed, Archived }
public enum TaskStatus      { Todo, InProgress, Review, Done, Cancelled }
public enum TaskPriority    { Low, Medium, High, Critical }
public enum PermissionLevel { View, Download, Edit }
public enum UserSystemRole  { SuperAdmin, TenantOwner, TenantAdmin, Employee }
```

---

## 4. DbContext & Cấu hình

```csharp
// Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentPermission> DocumentPermissions => Set<DocumentPermission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Load tất cả IEntityTypeConfiguration từ assembly này
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filter — soft delete + tenant isolation
        builder.Entity<ProjectTask>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Document>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Project>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Department>().HasQueryFilter(x => !x.IsDeleted);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
        return base.SaveChangesAsync(ct);
    }
}
```

**Quy tắc Migration:**
- Chạy migration với tên mô tả rõ: `Add-Migration AddProjectTaskAiFields`
- Không sửa migration đã apply vào production — tạo migration mới
- Mỗi migration phải có `Up()` và `Down()` hoàn chỉnh

---

## 5. Multi-Tenancy

### 5.1 Tenant Resolution

```csharp
// Infrastructure/Services/TenantService.cs
public interface ITenantService
{
    Guid? GetCurrentTenantId();
}

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public Guid? GetCurrentTenantId()
    {
        var claim = _httpContextAccessor.HttpContext?
            .User.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
```

### 5.2 Quy tắc Tenant Isolation

- **BẮT BUỘC:** Mọi query lên `TenantBaseEntity` phải có `.Where(x => x.TenantId == currentTenantId)`
- Không bao giờ trả về data cross-tenant
- SuperAdmin truy vấn không qua filter tenant — dùng `.IgnoreQueryFilters()` có chú thích rõ ràng
- Dùng `TenantRepository<T>` wrapper để enforce rule này tự động

```csharp
// Infrastructure/Repositories/TenantRepository.cs
public class TenantRepository<T> where T : TenantBaseEntity
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantService _tenantService;

    protected IQueryable<T> Query()
    {
        var tenantId = _tenantService.GetCurrentTenantId()
            ?? throw new UnauthorizedAccessException("Tenant context required");
        return _db.Set<T>().Where(x => x.TenantId == tenantId);
    }
}
```

---

## 6. RBAC — Phân quyền

### 6.1 Claims & Roles

JWT token phải chứa:
```json
{
  "sub": "userId",
  "tenant_id": "tenantId",
  "role": "TenantAdmin",
  "permissions": ["task.create", "task.assign", "report.view"]
}
```

### 6.2 Policy-based Authorization

```csharp
// Web/Program.cs — đăng ký policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly",
        p => p.RequireRole(nameof(UserSystemRole.SuperAdmin)));

    options.AddPolicy("CanCreateTask",
        p => p.RequireClaim("permissions", "task.create"));

    options.AddPolicy("CanAssignTask",
        p => p.RequireClaim("permissions", "task.assign"));

    options.AddPolicy("CanViewReport",
        p => p.RequireClaim("permissions", "report.view"));

    options.AddPolicy("TenantMember",
        p => p.RequireClaim("tenant_id"));
});
```

### 6.3 Controller Authorization

```csharp
// Luôn attribute ở class level, override ở action nếu cần
[Authorize(Policy = "TenantMember")]
public class TasksController : Controller
{
    [Authorize(Policy = "CanCreateTask")]
    public async Task<IActionResult> Create(CreateTaskDto dto) { ... }

    [Authorize(Policy = "CanAssignTask")]
    public async Task<IActionResult> Assign(AssignTaskDto dto) { ... }
}
```

**Quy tắc phân quyền theo role:**

| Action | SuperAdmin | TenantOwner | TenantAdmin | Employee |
|--------|-----------|-------------|-------------|----------|
| Quản lý tenant | ✓ | - | - | - |
| Cấu hình billing | ✓ | - | - | - |
| Vẽ org chart | - | ✓ | - | - |
| Thêm/xóa nhân sự | - | ✓ | - | - |
| Phân quyền tài liệu | - | ✓ | ✓ | - |
| Tạo & giao task AI | - | ✓ | ✓ | - |
| Quản lý dự án | - | ✓ | ✓ | - |
| Xem báo cáo nhóm | - | ✓ | ✓ | - |
| Thực hiện task | - | - | - | ✓ |
| Xem & tải tài liệu | - | - | - | ✓ (nếu có quyền) |
| Nhận thông báo | - | - | - | ✓ |

---

## 7. Service Layer

### 7.1 Quy tắc Service

- Mỗi feature có 1 Interface + 1 Implementation
- Service nhận DTO, trả về DTO — không bao giờ leak Entity ra ngoài service
- Dùng `Result<T>` pattern thay vì throw exception cho business errors

```csharp
// Application/Common/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}
```

### 7.2 AI Task Assignment Service

```csharp
// Application/Interfaces/IAiTaskService.cs
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
```

**Quy tắc khi gọi AI:**
- Luôn set timeout (tối đa 30 giây)
- Luôn có fallback nếu AI không trả lời
- Log toàn bộ prompt + response để debug
- Không đưa dữ liệu nhạy cảm (password, token) vào prompt
- Giới hạn token prompt — tóm tắt context thay vì dump toàn bộ data

**Ví dụ prompt chuẩn cho AI phân công task:**

```csharp
var prompt = $"""
Bạn là hệ thống phân công task thông minh cho công ty.

THÔNG TIN TASK:
- Tiêu đề: {task.Title}
- Mô tả: {task.Description}
- Độ ưu tiên: {task.Priority}
- Deadline: {task.DueDate:dd/MM/yyyy}

DANH SÁCH NHÂN VIÊN KHẢ DỤNG:
{employeeList}

YÊU CẦU:
Chọn 1 nhân viên phù hợp nhất. Trả lời JSON:
{{
  "assigneeId": "guid",
  "reason": "lý do ngắn gọn bằng tiếng Việt"
}}
Chỉ trả JSON, không giải thích thêm.
""";
```

---

## 8. Controller & View

### 8.1 Quy tắc Controller

```csharp
public class TasksController : Controller
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    // Constructor injection — không new() trực tiếp
    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    // Action trả View
    public async Task<IActionResult> Index()
    {
        var result = await _taskService.GetUserTasksAsync();
        if (!result.IsSuccess) return View("Error", result.Error);
        return View(result.Value);
    }

    // Action nhận form POST — dùng PRG pattern (Post-Redirect-Get)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTaskDto dto)
    {
        if (!ModelState.IsValid) return View(dto);
        var result = await _taskService.CreateAsync(dto);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(dto);
        }
        TempData["Success"] = "Task đã được tạo thành công";
        return RedirectToAction(nameof(Index));
    }

    // API endpoint trong MVC — dùng cho AJAX/fetch
    [HttpPost]
    public async Task<IActionResult> AssignWithAi([FromBody] AssignAiRequestDto dto)
    {
        var result = await _taskService.AiAssignAsync(dto);
        return result.IsSuccess
            ? Ok(result.Value)
            : BadRequest(new { error = result.Error });
    }
}
```

### 8.2 Quy tắc View

- Mọi View phải có `@model` declaration
- Dùng Tag Helpers thay vì HTML Helpers
- Form phải có `@Html.AntiForgeryToken()` hoặc `asp-antiforgery="true"`
- Không viết business logic trong Razor View

---

## 9. DTO & Validation

```csharp
// Application/DTOs/Tasks/CreateTaskDto.cs
public class CreateTaskDto
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(200, ErrorMessage = "Tiêu đề tối đa 200 ký tự")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public Guid ProjectId { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    public Guid? AssignedToId { get; set; }

    public bool UseAiAssignment { get; set; } = false;
}
```

**Quy tắc DTO:**
- Input DTO: tên kết thúc `Dto` hoặc `Request` — `CreateTaskDto`, `UpdateProjectRequest`
- Output DTO: tên kết thúc `Dto` hoặc `Response` — `TaskDetailDto`, `ProjectListItemDto`
- Không bao giờ dùng Entity trực tiếp làm tham số Action hoặc View model
- Dùng AutoMapper hoặc manual mapping — ghi rõ trong mapping profile

---

## 10. Error Handling

```csharp
// Web/Middleware/GlobalExceptionMiddleware.cs
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try { await next(context); }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access attempt");
            context.Response.StatusCode = 403;
            // redirect hoặc return JSON tùy request type
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
        }
    }
}
```

**Quy tắc:**
- Không để exception bay ra ngoài Controller chưa được xử lý
- Log mọi exception với context đủ để debug
- Không bao giờ trả message exception gốc ra client (production)
- HTTP 400 cho input lỗi, 401 chưa đăng nhập, 403 không có quyền, 404 không tìm thấy, 500 lỗi server

---

## 11. Logging & Monitoring

```csharp
// Dùng ILogger<T> — KHÔNG Console.WriteLine
_logger.LogInformation("Task {TaskId} assigned to {UserId} by AI. Reason: {Reason}",
    task.Id, assignee.Id, aiReason);

_logger.LogWarning("AI assignment failed for task {TaskId}: {Error}", task.Id, error);

_logger.LogError(ex, "Failed to process document {DocumentId}", documentId);
```

**Quy tắc log:**
- Log đủ context — luôn log Id của entity liên quan
- Không log password, token, nội dung tài liệu nhạy cảm
- Log AI prompt/response ở level `Debug` (tắt trong production nếu cần)

---

## 12. Naming Conventions tổng hợp

| Loại | Convention | Ví dụ |
|------|-----------|-------|
| Class | PascalCase | `ProjectTaskService` |
| Interface | IPascalCase | `IProjectTaskService` |
| Method | PascalCase | `GetByIdAsync` |
| Async method | Suffix `Async` | `CreateTaskAsync` |
| Private field | _camelCase | `_taskService` |
| Parameter | camelCase | `taskId` |
| Local variable | camelCase | `currentTask` |
| Constant | UPPER_SNAKE | `MAX_FILE_SIZE_MB` |
| Enum | PascalCase | `TaskStatus.InProgress` |
| DTO | PascalCase + Dto | `CreateTaskDto` |
| Controller | PascalCase + Controller | `TasksController` |
| Migration | Mô tả rõ ràng | `AddAiSummaryToDocuments` |

---

## 13. Checklist trước khi commit

- [ ] Không có entity nào thiếu `TenantId` (với TenantBaseEntity)
- [ ] Mọi query đều có tenant filter (hoặc có chú thích SuperAdmin override)
- [ ] Không có business logic trong Controller
- [ ] Không có Entity leak ra View (chỉ dùng DTO)
- [ ] Mọi action POST có `[ValidateAntiForgeryToken]`
- [ ] Migration mới có tên mô tả rõ ràng
- [ ] AI prompt không chứa data nhạy cảm
- [ ] Log đủ context, không log secret
- [ ] Async method có suffix `Async` và dùng `await` đúng cách
- [ ] Không có `Task.Result` hoặc `.Wait()` (gây deadlock)