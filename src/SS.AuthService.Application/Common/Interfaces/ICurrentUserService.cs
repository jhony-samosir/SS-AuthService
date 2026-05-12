namespace SS.AuthService.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserDisplayName { get; }
}
