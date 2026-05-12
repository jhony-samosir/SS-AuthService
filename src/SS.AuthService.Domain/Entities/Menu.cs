using System;
using System.Collections.Generic;
using SS.AuthService.Domain.Common;

namespace SS.AuthService.Domain.Entities;

public partial class Menu : IAuditableEntity, ISoftDelete
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public int? ParentId { get; set; }

    public string Name { get; set; } = null!;

    public string Path { get; set; } = null!;

    public string? Icon { get; set; }

    public short SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
    
    public virtual Menu? Parent { get; set; }
    public virtual ICollection<Menu> Children { get; set; } = new List<Menu>();
    public virtual ICollection<RoleMenu> RoleMenus { get; set; } = new List<RoleMenu>();
}
