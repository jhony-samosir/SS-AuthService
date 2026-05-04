using System;
using System.Collections.Generic;
using SS.AuthService.Domain.Common;

namespace SS.AuthService.Domain.Entities;

public partial class User : IAuditableEntity, ISoftDelete
{
    public int Id { get; set; }

    public Guid PublicId { get; set; }

    public int RoleId { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string FullName { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool MfaEnabled { get; set; }

    public string? MfaSecret { get; set; }

    public short FailedLoginAttempts { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime? EmailVerifiedAt { get; set; }

    public DateTime? TosAcceptedAt { get; set; }

    public DateTime? PrivacyPolicyAcceptedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? DeletedBy { get; set; }

    public virtual Role Role { get; set; } = null!;
}
