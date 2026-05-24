# OperaIQ System Progress Memory

## 1. Thông tin chung
- **Tên hệ thống**: OperaIQ
- **Kiến trúc**: 4 lớp (Web / Application / Domain / Infrastructure)
- **Công nghệ**: ASP.NET Core MVC .NET 10, Entity Framework Core Code First, SQL Server (Server: `ngocdieu`)
- **Phân quyền (RBAC)**: SuperAdmin, TenantOwner, TenantAdmin, Employee
- **Tích hợp AI**: Claude API Stub (Tự động gán tác vụ & Tóm tắt tài liệu)
- **Real-time**: SignalR cho thông báo
- **Tài liệu quy tắc**: `GEMINI.md` (đã đọc), `workflow-ai-rules.md` (giả định theo quy tắc đặt tên trong yêu cầu)

## 2. Trạng thái hiện tại
- Đang ở chế độ **Lập kế hoạch (Planning Mode)**.
- Đang tạo tài liệu Kế hoạch triển khai (`implementation_plan.md`) để người dùng phê duyệt trước khi viết mã.
- Thư mục dự án đã có sẵn một thư mục `OperaIQ` chứa mã nguồn MVC cơ bản ban đầu, chúng ta sẽ cấu trúc lại thành Solution chứa 4 dự án riêng biệt.

## 3. Các bước thực hiện kế hoạch
- [ ] Bước 1: Tạo Solution và 4 dự án (.Domain, .Application, .Infrastructure, .Web), thiết lập tham chiếu giữa các dự án.
- [ ] Bước 2: Định nghĩa các thực thể Domain (BaseEntity, TenantBaseEntity, Tenant, User, Role, Project, Task, Document, Department, Notification).
- [ ] Bước 3: Cấu hình DbContext (AppDbContext) với global query filters cho Tenant và override SaveChangesAsync để tự động gán TenantId, Auditing.
- [ ] Bước 4: Đăng ký dịch vụ trong Program.cs (Identity, JWT, EF Core, RBAC policies, SignalR, ITenantService).
- [ ] Bước 5: Triển khai TenantService và TenantRepository<T>.
- [ ] Bước 6: Xây dựng tầng Application (interfaces, DTOs, Result<T>, AutoMapper profiles).
- [ ] Bước 7: Triển khai các dịch vụ: TaskService, ProjectService, DocumentService, AiTaskService (stub).
- [ ] Bước 8: Tạo các Controllers với các thuộc tính [Authorize(Policy)] thích hợp.
- [ ] Bước 9: Tạo các Razor Views sử dụng DTOs thay vì Entity trực tiếp.
- [ ] Bước 10: Thêm SignalR NotificationHub và tích hợp vào các nghiệp vụ cần thiết.
- [ ] Bước 11: Tạo EF Core Migration đầu tiên và thực hiện Seed dữ liệu (1 SuperAdmin, 1 demo Tenant với Owner + Admin + 2 Employees).
- [ ] Bước 12: Biên dịch và kiểm tra lỗi hệ thống.
