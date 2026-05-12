using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SS.AuthService.Application.Common.Interfaces;
using SS.AuthService.Domain.Common;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SS.AuthService.Infrastructure.Persistence.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var userDisplayName = _currentUserService.UserDisplayName;
        var now = DateTime.UtcNow;

        // 1. Audit Trail Logic (IAuditableEntity)
        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userDisplayName;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userDisplayName;
            }
            else if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userDisplayName;
            }
        }

        // 2. Soft Delete Logic (ISoftDelete)
        foreach (var entry in context.ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                // Batalkan hard delete, ubah ke Unchanged agar tidak mengupdate semua kolom
                entry.State = EntityState.Unchanged;

                entry.Entity.DeletedAt = now;
                entry.Entity.DeletedBy = userDisplayName;

                // Tandai hanya kolom audit yang berubah (Efficient SQL)
                entry.Property(nameof(ISoftDelete.DeletedAt)).IsModified = true;
                entry.Property(nameof(ISoftDelete.DeletedBy)).IsModified = true;

                // Jika entitas juga IAuditableEntity, update jejak terakhirnya
                if (entry.Entity is IAuditableEntity auditable)
                {
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = userDisplayName;
                    entry.Property(nameof(IAuditableEntity.UpdatedAt)).IsModified = true;
                    entry.Property(nameof(IAuditableEntity.UpdatedBy)).IsModified = true;
                }
            }
        }
    }
}

public static class EntityEntryExtensions
{
    public static bool HasChangedOwnedEntities(this Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.References.Any(r => 
            r.TargetEntry != null && 
            r.TargetEntry.Metadata.IsOwned() && 
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
