using System.Threading;
using System.Threading.Tasks;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken);
}
