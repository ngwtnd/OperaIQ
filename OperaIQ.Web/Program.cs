using System;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using OperaIQ.Application.Common;
using OperaIQ.Application.Mappings;
using OperaIQ.Application.Services;
using OperaIQ.Domain.Entities;
using OperaIQ.Domain.Enums;
using OperaIQ.Infrastructure.Data;
using OperaIQ.Infrastructure.Hubs;
using OperaIQ.Infrastructure.Repositories;
using OperaIQ.Infrastructure.Services;
using OperaIQ.Web.Middlewares;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Serilog Ghi Log hệ thống
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/operaiq_log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// 2. Cấu hình Chuỗi kết nối SQL Server
string connectionString = "Server=ngocdieu;Database=OperaIQDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("OperaIQ.Infrastructure")));

// 3. Đăng ký ASP.NET Core Identity (Sử dụng AppUser và IdentityRole<Guid>)
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 4. Cấu hình xác thực (Cookie cho MVC và JWT cho API/SignalR)
string jwtSecret = builder.Configuration["Jwt:Secret"] ?? "OperaIQSuperSecretSecurityKey1234567890!!!";
var jwtKey = Encoding.ASCII.GetBytes(jwtSecret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// 5. Cấu hình phân quyền RBAC Policies theo rule.md §6.2
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

// 6. Đăng ký DI cho các lớp thuộc tầng Application & Infrastructure
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);

builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped(typeof(ITenantRepository<>), typeof(TenantRepository<>));

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IAiTaskService, AiTaskService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// 7. Đăng ký SignalR cho Real-time Notifications
builder.Services.AddSignalR();

// 8. Đăng ký Hangfire cho Tác vụ chạy nền (Background Jobs)
builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();

// Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Chạy Tenant Resolution Middleware sau Routing và trước Authentication
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

// Seed Dữ liệu Hệ thống
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await DbInitializer.SeedAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi khi seed dữ liệu.");
    }
}

app.Run();
