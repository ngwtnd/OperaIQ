using System;
using System.ComponentModel.DataAnnotations;

namespace OperaIQ.Application.DTOs
{
    public class DocumentDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string? AiSummary { get; set; }
        
        public Guid UploadedById { get; set; }
        public string? UploadedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UploadDocumentDto
    {
        [Required(ErrorMessage = "Tên tài liệu không được để trống")]
        public string Name { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }
}
