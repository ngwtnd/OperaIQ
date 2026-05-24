using System;
using Microsoft.AspNetCore.Identity;

namespace OperaIQ.Domain.Entities
{
    public class UserRole : IdentityUserRole<Guid>
    {
        public virtual AppUser User { get; set; } = null!;
        public virtual IdentityRole<Guid> Role { get; set; } = null!;
    }
}
