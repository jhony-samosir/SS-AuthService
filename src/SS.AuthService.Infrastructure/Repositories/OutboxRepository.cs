using System.Threading;
using System.Threading.Tasks;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Domain.Entities;
using SS.AuthService.Infrastructure.Persistence.Context;

namespace SS.AuthService.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _dbContext;

    public OutboxRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken)
    {
        await _dbContext.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
    }
}
