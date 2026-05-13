using MediatR;
using Microsoft.Extensions.Logging;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.RoleMenus.Commands;
using SS.AuthService.Domain.Entities;
using System.Text.Json;

namespace SS.AuthService.Application.RoleMenus.Handlers;

public class SyncRolePermissionsCommandHandler : IRequestHandler<SyncRolePermissionsCommand, Result<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly IRoleMenuRepository _roleMenuRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncRolePermissionsCommandHandler> _logger;

    public SyncRolePermissionsCommandHandler(
        IRoleRepository roleRepository,
        IMenuRepository menuRepository,
        IRoleMenuRepository roleMenuRepository,
        IUnitOfWork unitOfWork,
        ILogger<SyncRolePermissionsCommandHandler> logger)
    {
        _roleRepository = roleRepository;
        _menuRepository = menuRepository;
        _roleMenuRepository = roleMenuRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(SyncRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByPublicIdAsync(request.RolePublicId, cancellationToken);
        if (role == null) return Result<bool>.Failure("RoleNotFound", "Role not found.");

        // 1. Fetch all menus in a single batch
        var menuPublicIds = request.Permissions.Select(p => p.MenuId).Distinct().ToList();
        var menus = await _menuRepository.GetByPublicIdsAsync(menuPublicIds, cancellationToken);
        var menuMap = menus.ToDictionary(m => m.PublicId);

        // 2. Validate all menus exist
        var missingMenuIds = menuPublicIds.Where(id => !menuMap.ContainsKey(id)).ToList();
        if (missingMenuIds.Any())
        {
            return Result<bool>.Failure("InvalidMenus", 
                $"The following MenuPublicIds were not found: {string.Join(", ", missingMenuIds)}");
        }

        // 3. Get current permissions for audit/comparison
        var currentPermissions = await _roleMenuRepository.GetByRoleIdAsync(role.Id, cancellationToken);

        _logger.LogInformation("Syncing permissions for Role {RoleName} ({RolePublicId}). Current count: {CurrentCount}, New count: {NewCount}", 
            role.Name, role.PublicId, currentPermissions.Count, request.Permissions.Count);

        // 4. Remove all current mappings
        _roleMenuRepository.RemoveRange(currentPermissions);

        // 5. Add new mappings with invariants
        foreach (var input in request.Permissions)
        {
            var menu = menuMap[input.MenuId];

            // Invariant: If any action is allowed, CanRead must be true
            bool canRead = input.CanRead || input.CanCreate || input.CanUpdate || input.CanDelete;

            var newPerm = new RoleMenu
            {
                RoleId = role.Id,
                MenuId = menu.Id,
                CanCreate = input.CanCreate,
                CanRead = canRead,
                CanUpdate = input.CanUpdate,
                CanDelete = input.CanDelete
            };

            _roleMenuRepository.Add(newPerm);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Invalidate Cache
        _roleMenuRepository.InvalidateCache(role.Id);

        _logger.LogInformation("Successfully synced permissions for Role {RoleName}. Permissions: {Permissions}", 
            role.Name, JsonSerializer.Serialize(request.Permissions));

        return Result<bool>.Success(true);
    }
}
