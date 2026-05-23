using MediatR;
using SS.AuthService.Application.Auth.Commands;
using SS.AuthService.Application.Auth.DTOs;
using SS.AuthService.Application.Interfaces;

namespace SS.AuthService.Application.Auth.Handlers;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenHasher _tokenHasher;
    private readonly IOutboxRepository _outboxRepository;

    public VerifyEmailCommandHandler(IUnitOfWork unitOfWork, ITokenHasher tokenHasher, IOutboxRepository outboxRepository)
    {
        _unitOfWork = unitOfWork;
        _tokenHasher = tokenHasher;
        _outboxRepository = outboxRepository;
    }

    public async Task<VerifyEmailResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenHasher.Hash(request.Token);
        var verification = await _unitOfWork.EmailVerifications.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (verification == null)
        {
            return VerifyEmailResult.TokenNotFound;
        }

        if (verification.ExpiresAt < DateTime.UtcNow)
        {
            return VerifyEmailResult.TokenExpired;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(verification.UserId, cancellationToken);
        if (user == null)
        {
            return VerifyEmailResult.UserNotFound;
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            user.EmailVerifiedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(user);
            _unitOfWork.EmailVerifications.Remove(verification);

            var outboxEvent = new SS.AuthService.Domain.Entities.OutboxEvent
            {
                EventType = "UserVerified",
                AggregateType = "User",
                AggregateId = user.Id,
                Payload = System.Text.Json.JsonSerializer.Serialize(new 
                { 
                    userId = user.Id,
                    publicId = user.PublicId,
                    email = user.Email
                }),
            };
            await _outboxRepository.AddAsync(outboxEvent, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return VerifyEmailResult.Success;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
