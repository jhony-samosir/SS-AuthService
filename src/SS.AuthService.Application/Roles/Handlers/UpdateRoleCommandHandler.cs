using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Roles.Commands;

namespace SS.AuthService.Application.Roles.Handlers;

public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, Result<bool>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByPublicIdAsync(request.PublicId, cancellationToken);
        if (role == null)
        {
            return Result<bool>.Failure("RoleNotFound", "Role not found.");
        }

        var trimmedName = request.Name.Trim();

        if (await _roleRepository.ExistsByNameAsync(trimmedName, role.Id, cancellationToken))
        {
            return Result<bool>.Failure("RoleNameAlreadyExists", $"Role with name '{trimmedName}' already exists.");
        }

        role.Name = trimmedName;
        role.Description = request.Description;

        _roleRepository.Update(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
