using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;
using OperaIQ.Domain.Entities;

namespace OperaIQ.Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ITenantRepository<Document> _documentRepo;
        private readonly IAiTaskService _aiTaskService;
        private readonly ITenantService _tenantService;
        private readonly IMapper _mapper;

        public DocumentService(
            ITenantRepository<Document> documentRepo,
            IAiTaskService aiTaskService,
            ITenantService tenantService,
            IMapper mapper)
        {
            _documentRepo = documentRepo;
            _aiTaskService = aiTaskService;
            _tenantService = tenantService;
            _mapper = mapper;
        }

        public async Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync()
        {
            var documents = await _documentRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<DocumentDto>>(documents);
        }

        public async Task<DocumentDto?> GetDocumentByIdAsync(Guid id)
        {
            var document = await _documentRepo.GetByIdAsync(id);
            if (document == null) return null;
            return _mapper.Map<DocumentDto>(document);
        }

        public async Task<Result<Guid>> UploadDocumentAsync(UploadDocumentDto dto, Guid userId)
        {
            var document = _mapper.Map<Document>(dto);
            document.UploadedById = userId;

            await _documentRepo.AddAsync(document);
            await _documentRepo.SaveChangesAsync();

            return Result.Success(document.Id);
        }

        public async Task<Result> SummarizeDocumentWithAiAsync(Guid documentId)
        {
            var document = await _documentRepo.GetByIdAsync(documentId);
            if (document == null) return Result.Failure("Không tìm thấy tài liệu.");

            Guid tenantId = _tenantService.TenantId ?? Guid.Empty;
            if (tenantId == Guid.Empty) return Result.Failure("Không thể xác định Context Tenant.");

            // Trong thực tế sẽ đọc nội dung file văn bản, ở đây ta giả lập nội dung text từ file
            string sampleContentText = $"Đây là tài liệu: {document.Name}. Loại tệp: {document.ContentType}. Kích thước: {document.FileSizeBytes} bytes.\n" +
                                       "Nội dung tài liệu xoay quanh việc triển khai hệ thống quản trị OperaIQ, cấu hình phân phối tải công việc thông minh bằng AI Claude và tích hợp thông báo đẩy SignalR nhằm đảm bảo tính an toàn dữ liệu Multi-tenancy SaaS.";

            // Gọi AI Service để tóm tắt tài liệu
            var aiResult = await _aiTaskService.SummarizeDocumentAsync(sampleContentText);
            if (!aiResult.IsSuccess || string.IsNullOrEmpty(aiResult.Value))
            {
                return Result.Failure($"Lỗi gọi AI tóm tắt tài liệu: {aiResult.Error}");
            }

            document.AiSummary = aiResult.Value;
            _documentRepo.Update(document);
            await _documentRepo.SaveChangesAsync();

            return Result.Success();
        }
    }
}
