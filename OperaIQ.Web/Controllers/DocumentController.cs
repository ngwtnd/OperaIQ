using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperaIQ.Application.DTOs;
using OperaIQ.Application.Services;
using OperaIQ.Domain.Entities;
using OperaIQ.Domain.Enums;
using OperaIQ.Infrastructure.Data;

namespace OperaIQ.Web.Controllers
{
    [Authorize(Policy = "TenantMember")]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;

        public DocumentController(
            IDocumentService documentService,
            IWebHostEnvironment env,
            ApplicationDbContext context)
        {
            _documentService = documentService;
            _env = env;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var docs = await _documentService.GetAllDocumentsAsync();
            return View(docs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn một tệp hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Tạo thư mục uploads nếu chưa có
                var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Lưu tệp vật lý
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Ghi nhận vào DB
                var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
                var uploadDto = new UploadDocumentDto
                {
                    Name = file.FileName,
                    StoragePath = "/uploads/" + uniqueFileName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length
                };

                var result = await _documentService.UploadDocumentAsync(uploadDto, userId);
                if (result.IsSuccess)
                {
                    TempData["Success"] = "Tài liệu đã được tải lên thành công!";
                }
                else
                {
                    TempData["Error"] = $"Tải lên thất bại: {result.Error}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi hệ thống: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Summarize(Guid id)
        {
            var result = await _documentService.SummarizeDocumentWithAiAsync(id);
            if (result.IsSuccess)
            {
                var doc = await _context.Documents.FindAsync(id);
                return Json(new { success = true, summary = doc?.AiSummary });
            }
            return Json(new { success = false, error = result.Error });
        }

        [HttpGet]
        public async Task<IActionResult> Download(Guid id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null || doc.IsDeleted)
            {
                return NotFound();
            }

            // Kiểm tra phân quyền tài liệu trước khi cho tải xuống
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            if (User.IsInRole(nameof(UserSystemRole.Employee)))
            {
                // Kiểm tra DocumentPermission
                var hasPermission = await _context.DocumentPermissions
                    .AnyAsync(dp => dp.DocumentId == id && 
                                    ((dp.UserId == Guid.Parse(userIdStr!) && dp.Level >= PermissionLevel.Download) ||
                                     (dp.RoleName == userRole && dp.Level >= PermissionLevel.Download)));

                // Nếu là tài liệu của chính mình tải lên thì luôn được xem/tải
                if (!hasPermission && doc.UploadedById != Guid.Parse(userIdStr!))
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }

            var filePath = Path.Combine(_env.WebRootPath, doc.StoragePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File không tồn tại trên máy chủ.");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, doc.ContentType, doc.Name);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            // Chỉ Owner, Admin hoặc chính người upload được xóa
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            if (!User.IsInRole(nameof(UserSystemRole.TenantOwner)) && 
                !User.IsInRole(nameof(UserSystemRole.TenantAdmin)) && 
                doc.UploadedById != userId)
            {
                TempData["Error"] = "Bạn không có quyền xóa tài liệu này.";
                return RedirectToAction(nameof(Index));
            }

            doc.IsDeleted = true;
            doc.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa tài liệu thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
