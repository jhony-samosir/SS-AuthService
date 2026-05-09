using System.Threading.Tasks;

namespace SS.AuthService.Application.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string token);
    Task SendMfaRecoveryCodesEmailAsync(string email, IEnumerable<string> codes);
}
