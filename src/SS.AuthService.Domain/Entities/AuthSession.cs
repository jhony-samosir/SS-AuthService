using System.Net;
using SS.AuthService.Domain.Common;

namespace SS.AuthService.Domain.Entities;

public partial class AuthSession : IAuditableEntity
{
    public long Id { get; set; }

    public Guid PublicId { get; set; }

    public int UserId { get; set; }

    public string RefreshTokenHash { get; set; } = null!;

    public string? DeviceInfo { get; set; }

    public IPAddress? IpAddress { get; set; }

    public DateTime ExpiresAt { get; set; }

    public bool IsRevoked { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
