using System;
using System.Collections.Generic;
using SS.AuthService.Domain.Common;

namespace SS.AuthService.Domain.Entities;

public partial class Role : IAuditableEntity, ISoftDelete
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
    
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
}
