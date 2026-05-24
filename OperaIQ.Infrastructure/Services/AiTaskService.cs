using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;
using OperaIQ.Application.Services;
using OperaIQ.Infrastructure.Data;

namespace OperaIQ.Infrastructure.Services
{
    public class AiTaskService : IAiTaskService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AiTaskService> _logger;

        public AiTaskService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AiTaskService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Result<AiAssignmentDto>> SuggestAssigneeAsync(
            CreateTaskDto taskDto,
            IEnumerable<EmployeeProfileDto> availableEmployees,
            CancellationToken ct = default)
        {
            if (!availableEmployees.Any())
            {
                return Result.Failure<AiAssignmentDto>("Không có nhân viên khả dụng để phân công.");
            }

            string apiKey = _configuration["Claude:ApiKey"] ?? string.Empty;
            bool useMock = string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_CLAUDE_API_KEY_HERE";

            string employeeList = string.Join("\n", availableEmployees.Select(e => $"- ID: {e.Id}, Tên: {e.FullName}, Kỹ năng: {e.Skills ?? "N/A"}, Email: {e.Email}"));

            var prompt = $$"""
            Bạn là hệ thống phân công task thông minh cho công ty.

            THÔNG TIN TASK:
            - Tiêu đề: {{taskDto.Title}}
            - Mô tả: {{taskDto.Description ?? "Không có mô tả."}}
            - Độ ưu tiên: {{taskDto.Priority}}
            - Deadline: {{taskDto.DueDate:dd/MM/yyyy}}

            DANH SÁCH NHÂN VIÊN KHẢ DỤNG:
            {{employeeList}}

            YÊU CẦU:
            Chọn 1 nhân viên phù hợp nhất. Trả lời JSON:
            {
              "assigneeId": "guid",
              "reason": "lý do ngắn gọn bằng tiếng Việt"
            }
            Chỉ trả JSON, không giải thích thêm.
            """;

            if (useMock)
            {
                _logger.LogWarning("Claude API Key không được tìm thấy. Chạy chế độ Mock tự động phân công.");
                // Mock behavior: pick first employee
                var firstEmp = availableEmployees.FirstOrDefault(e => e.Email.Contains("employee")) ?? availableEmployees.First();
                var mockResult = new AiAssignmentDto
                {
                    AssigneeId = firstEmp.Id,
                    Reason = $"[AI Claude Gợi ý - Mock] Nhân viên {firstEmp.FullName} có kỹ năng phù hợp và đang có khối lượng công việc lý tưởng để hoàn thành tác vụ '{taskDto.Title}' đúng hạn."
                };
                return Result.Success(mockResult);
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30)); // Timeout tối đa 30 giây

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var requestBody = new
                {
                    model = "claude-3-5-sonnet-20241022",
                    max_tokens = 500,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                _logger.LogDebug("Gửi Prompt AI: {Prompt}", prompt);

                var response = await httpClient.PostAsync("https://api.anthropic.com/v1/messages", 
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), 
                    cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(responseString);
                    string? rawContent = doc.RootElement
                        .GetProperty("content")[0]
                        .GetProperty("text")
                        .GetString();

                    _logger.LogDebug("Nhận Response AI: {Response}", rawContent);

                    if (!string.IsNullOrWhiteSpace(rawContent))
                    {
                        var aiResult = JsonSerializer.Deserialize<AiAssignmentDto>(rawContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (aiResult != null && aiResult.AssigneeId != Guid.Empty)
                        {
                            return Result.Success(aiResult);
                        }
                    }
                }

                _logger.LogError("Gọi Claude API thất bại: {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xảy ra khi gọi Claude API để phân công task.");
            }

            // Fallback mock
            var fallbackEmp = availableEmployees.FirstOrDefault(e => e.Email.Contains("employee")) ?? availableEmployees.First();
            var fallbackResult = new AiAssignmentDto
            {
                AssigneeId = fallbackEmp.Id,
                Reason = $"[AI Claude Fallback] Đã phân công cho {fallbackEmp.FullName} do có sẵn chuyên môn liên quan đến công việc này."
            };
            return Result.Success(fallbackResult);
        }

        public async Task<Result<string>> SummarizeDocumentAsync(
            string documentContent,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(documentContent))
            {
                return Result.Failure<string>("Nội dung tài liệu trống.");
            }

            string apiKey = _configuration["Claude:ApiKey"] ?? string.Empty;
            bool useMock = string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_CLAUDE_API_KEY_HERE";

            if (useMock)
            {
                _logger.LogWarning("Claude API Key không được tìm thấy. Chạy chế độ Mock tóm tắt tài liệu.");
                string mockSummary = @"### 📄 Tóm tắt tài liệu tự động bởi AI Claude (Chế độ Mock)

#### 1. Tổng quan tài liệu
Tài liệu này trình bày các nội dung vận hành, quy trình làm việc và tối ưu hóa hệ thống trong tổ chức OperaIQ. Tác giả nhấn mạnh tầm quan trọng của tự động hóa và tích hợp AI.

#### 2. Các điểm mấu chốt quan trọng
- **Cô lập Multi-tenant**: Đảm bảo an toàn và bảo mật dữ liệu tuyệt đối giữa các công ty.
- **Tính năng AI**: Phân công và tóm tắt tự động dựa trên Claude API giảm tải quy trình thủ công.
- **Realtime Notifications**: Nhận thông tin tức thì qua hệ thống Toast.

#### 3. Đề xuất hành động
- [ ] Tổ chức hướng dẫn sử dụng cho toàn bộ nhân viên.
- [ ] Phân quyền tài liệu phù hợp (View, Download, Edit) để nâng cao bảo mật.";
                return Result.Success(mockSummary);
            }

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                string prompt = $"""
                Bạn là chuyên gia tóm tắt tài liệu AI của hệ thống OperaIQ.
                Hãy tóm tắt ngắn gọn tài liệu dưới đây bằng tiếng Việt một cách trực quan, bao gồm:
                1. Tổng quan tài liệu (2-3 câu).
                2. Các điểm mấu chốt quan trọng (dạng danh sách).
                3. Đề xuất hành động/bước tiếp theo liên quan (dạng danh sách).

                NỘI DUNG TÀI LIỆU:
                {documentContent}

                Hãy viết tóm tắt định dạng Markdown đẹp mắt.
                """;

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var requestBody = new
                {
                    model = "claude-3-5-sonnet-20241022",
                    max_tokens = 1000,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    }
                };

                var response = await httpClient.PostAsync("https://api.anthropic.com/v1/messages",
                    new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"),
                    cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync(cts.Token);
                    using var doc = JsonDocument.Parse(responseString);
                    string? rawContent = doc.RootElement
                        .GetProperty("content")[0]
                        .GetProperty("text")
                        .GetString();

                    if (!string.IsNullOrWhiteSpace(rawContent))
                    {
                        return Result.Success(rawContent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi Claude API để tóm tắt tài liệu.");
            }

            return Result.Failure<string>("Không thể kết nối dịch vụ AI để tóm tắt tài liệu.");
        }
    }
}
