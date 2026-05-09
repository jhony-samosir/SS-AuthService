using System;
using System.Collections.Generic;

namespace SS.AuthService.Application.Menus.DTOs;

public record MenuTreeDto(
    Guid PublicId,
    string Name,
    string Path,
    string? Icon,
    short SortOrder,
    List<MenuTreeDto> Children
);
