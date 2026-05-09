namespace SS.AuthService.Application.Interfaces;

public enum EmailType { Verification, MfaRecoveryCodes }
public record EmailTask(string To, string? Token, IEnumerable<string>? Codes, EmailType Type);

public interface IEmailQueue
{
    ValueTask QueueEmailAsync(EmailTask emailTask);
    ValueTask<EmailTask> DequeueEmailAsync(CancellationToken cancellationToken);
}
