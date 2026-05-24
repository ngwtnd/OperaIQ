using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OperaIQ.Application.Common;
using OperaIQ.Application.DTOs;

namespace OperaIQ.Application.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<DocumentDto>> GetAllDocumentsAsync();
        Task<DocumentDto?> GetDocumentByIdAsync(Guid id);
        Task<Result<Guid>> UploadDocumentAsync(UploadDocumentDto dto, Guid userId);
        Task<Result> SummarizeDocumentWithAiAsync(Guid documentId); // Tự động tóm tắt bằng AI
    }
}
