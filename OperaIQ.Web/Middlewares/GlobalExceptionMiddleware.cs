using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace OperaIQ.Web.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                context.Response.StatusCode = 403;
                if (IsApiRequest(context))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Bạn không có quyền truy cập tài nguyên này.\"}");
                }
                else
                {
                    context.Response.Redirect("/Account/AccessDenied");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                context.Response.StatusCode = 500;
                if (IsApiRequest(context))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.\"}");
                }
                else
                {
                    context.Response.Redirect("/Home/Error");
                }
            }
        }

        private static bool IsApiRequest(HttpContext context)
        {
            return context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                   context.Request.Path.StartsWithSegments("/api");
        }
    }
}
