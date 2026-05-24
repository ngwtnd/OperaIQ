using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaIQ.Application.DTOs;
using OperaIQ.Domain.Entities;
using OperaIQ.Domain.Enums;
using OperaIQ.Infrastructure.Data;

namespace OperaIQ.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            SignInManager<AppUser> signInManager,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.Tenants = await _context.Tenants.IgnoreQueryFilters().ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto, Guid? tenantId, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            var tenants = await _context.Tenants.IgnoreQueryFilters().ToListAsync();
            ViewBag.Tenants = tenants;

            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null || user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
                return View(dto);
            }

            // Nếu không phải SuperAdmin thì bắt buộc phải chọn Tenant hoặc có TenantId
            if (user.TenantId == null)
            {
                // Kiểm tra xem có thuộc SuperAdmin không
                var isSuperAdmin = await _userManager.IsInRoleAsync(user, nameof(UserSystemRole.SuperAdmin));
                if (!isSuperAdmin)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn không hợp lệ.");
                    return View(dto);
                }
            }
            else if (tenantId.HasValue && user.TenantId != tenantId.Value)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn không thuộc công ty đã chọn.");
                return View(dto);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, dto.Password, dto.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                // Tái cấu trúc Claims Principal để thêm các claims tùy chỉnh như tenant_id, permissions
                var userClaims = await _userManager.GetClaimsAsync(user);
                var roles = await _userManager.GetRolesAsync(user);
                
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email!)
                };

                if (user.TenantId.HasValue)
                {
                    claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));
                    var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == user.TenantId.Value);
                    if (tenant != null)
                    {
                        claims.Add(new Claim("tenant_slug", tenant.Slug));
                    }
                }

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                // Gán quyền cụ thể dựa trên Role theo rule.md §6.1 / §6.2
                if (roles.Contains(nameof(UserSystemRole.SuperAdmin)))
                {
                    claims.Add(new Claim("permissions", "tenant.manage"));
                    claims.Add(new Claim("permissions", "billing.configure"));
                }
                if (roles.Contains(nameof(UserSystemRole.TenantOwner)))
                {
                    claims.Add(new Claim("permissions", "orgchart.draw"));
                    claims.Add(new Claim("permissions", "employee.manage"));
                    claims.Add(new Claim("permissions", "document.share"));
                    claims.Add(new Claim("permissions", "task.create"));
                    claims.Add(new Claim("permissions", "task.assign"));
                    claims.Add(new Claim("permissions", "project.manage"));
                    claims.Add(new Claim("permissions", "report.view"));
                }
                if (roles.Contains(nameof(UserSystemRole.TenantAdmin)))
                {
                    claims.Add(new Claim("permissions", "document.share"));
                    claims.Add(new Claim("permissions", "task.create"));
                    claims.Add(new Claim("permissions", "task.assign"));
                    claims.Add(new Claim("permissions", "project.manage"));
                    claims.Add(new Claim("permissions", "report.view"));
                }
                if (roles.Contains(nameof(UserSystemRole.Employee)))
                {
                    claims.Add(new Claim("permissions", "task.execute"));
                    claims.Add(new Claim("permissions", "document.view"));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError(string.Empty, "Đăng nhập thất bại. Kiểm tra lại thông tin.");
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Departments = await _context.Departments.IgnoreQueryFilters().ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Departments = await _context.Departments.IgnoreQueryFilters().ToListAsync();
                return View(dto);
            }

            // Mặc định đăng ký cho nhân viên thuộc cùng Tenant với Admin hiện tại đang tạo tài khoản,
            // hoặc nếu là đăng ký tự do, gán vào Tenant mặc định
            Guid? tenantId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var tenantIdClaim = User.FindFirst("tenant_id")?.Value;
                if (Guid.TryParse(tenantIdClaim, out var tid))
                {
                    tenantId = tid;
                }
            }

            // Nếu Admin của Tenant đang tạo tài khoản, bắt buộc gán TenantId của Admin đó
            var user = new AppUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                TenantId = tenantId,
                DepartmentId = dto.DepartmentId,
                AvatarUrl = dto.AvatarUrl ?? "/images/default-avatar.png"
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (result.Succeeded)
            {
                // Gán Role
                if (!await _roleManager.RoleExistsAsync(dto.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole<Guid>(dto.Role));
                }
                await _userManager.AddToRoleAsync(user, dto.Role);

                TempData["Success"] = "Đăng ký tài khoản thành công!";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.Departments = await _context.Departments.IgnoreQueryFilters().ToListAsync();
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
