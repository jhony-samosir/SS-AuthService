using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Roles.Commands;

namespace SS.AuthService.Application.Roles.Handlers;

public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, Result<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (role == null)
        {
            return Result<bool>.Failure("RoleNotFound", "Role not found.");
        }

        if (await _roleRepository.HasUsersAsync(role.Id, cancellationToken))
        {
            return Result<bool>.Failure("RoleInUse", "Cannot delete role because it is still assigned to users.");
        }

        if (await _roleRepository.HasPermissionsAsync(role.Id, cancellationToken))
        {
            return Result<bool>.Failure("RoleHasPermissions", "Cannot delete role because it still has menu permission mappings.");
        }

        _roleRepository.Delete(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
