using SS.AuthService.Domain.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Application.Interfaces;

public interface IMfaRecoveryCodeRepository
{
    Task AddRangeAsync(IEnumerable<MfaRecoveryCode> codes, CancellationToken cancellationToken = default);
    Task<MfaRecoveryCode?> GetByHashAsync(string hash, CancellationToken cancellationToken = default);
    void Update(MfaRecoveryCode code);
    Task RemoveAllByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> CountByUserIdAsync(int userId, CancellationToken cancellationToken = default);
}
