using System;
using System.Collections.Generic;
using SS.AuthService.Domain.Common;

namespace SS.AuthService.Domain.Entities;

public partial class RoleMenu : IAuditableEntity, ISoftDelete
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int MenuId { get; set; }

    public bool CanCreate { get; set; }

    public bool CanRead { get; set; }

    public bool CanUpdate { get; set; }

    public bool CanDelete { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
    
    public virtual Role Role { get; set; } = null!;
    public virtual Menu Menu { get; set; } = null!;
}
