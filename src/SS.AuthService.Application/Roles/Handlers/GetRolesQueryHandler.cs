using MediatR;
using SS.AuthService.Application.Common.Models;
using SS.AuthService.Application.Interfaces;
using SS.AuthService.Application.Roles.DTOs;
using SS.AuthService.Application.Roles.Queries;

namespace SS.AuthService.Application.Roles.Handlers;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, PagedResult<RoleListItemDto>>
{
    private readonly IRoleRepository _roleRepository;

    public GetRolesQueryHandler(IRoleRepository roleRepository)
    {
        _roleRepository = roleRepository;
    }

    public async Task<PagedResult<RoleListItemDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _roleRepository.GetPagedAsync(request, cancellationToken);

        var dtos = items.Select(r => new RoleListItemDto(
            r.PublicId,
            r.Name,
            r.Description,
            r.CreatedAt
        )).ToList();

        return new PagedResult<RoleListItemDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
