using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Roles.Commands;
using SS.AuthService.Domain.Entities;

namespace SS.AuthService.Application.Roles.Handlers;

public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    private readonly IRoleRepository _roleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRoleCommandHandler(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        _roleRepository = roleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var trimmedName = request.Name.Trim();

        if (await _roleRepository.ExistsByNameAsync(trimmedName, null, cancellationToken))
        {
            return Result<Guid>.Failure("RoleNameAlreadyExists", $"Role with name '{trimmedName}' already exists.");
        }

        var role = new Role
        {
            PublicId = Guid.NewGuid(),
            Name = trimmedName,
            Description = request.Description
        };

        await _roleRepository.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(role.PublicId);
    }
}
