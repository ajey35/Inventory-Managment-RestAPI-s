namespace Inventory.Api.DTOs;

public sealed record CategoryResponse(Guid Id, string Name, string? Description);
