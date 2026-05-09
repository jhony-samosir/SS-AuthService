using MediatR;
using SS.AuthService.Application.Menus.DTOs;

namespace SS.AuthService.Application.Menus.Queries;

public record GetMenusQuery() : IRequest<List<MenuDto>>;
