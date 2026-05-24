using System;
using OperaIQ.Domain.Common;
using OperaIQ.Domain.Enums;

namespace OperaIQ.Domain.Entities
{
    public class DocumentPermission : BaseEntity
    {
        public Guid DocumentId { get; set; }
        public Guid? UserId { get; set; }                         // null = áp cho role
        public string? RoleName { get; set; }
        public PermissionLevel Level { get; set; }                // View, Download, Edit
        
        public virtual Document Document { get; set; } = null!;
        public virtual AppUser? User { get; set; }
    }
}
