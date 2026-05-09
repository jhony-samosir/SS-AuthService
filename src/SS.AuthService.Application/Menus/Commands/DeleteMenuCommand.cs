using MediatR;

namespace SS.AuthService.Application.Menus.Commands;

public record DeleteMenuCommand(Guid PublicId) : IRequest<bool>;
