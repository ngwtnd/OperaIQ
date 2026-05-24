using System;
using System.Collections.Generic;
using OperaIQ.Domain.Common;

namespace OperaIQ.Domain.Entities
{
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
}
