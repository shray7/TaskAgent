namespace TaskAgent.Contracts.Dtos;

public record UserDto(
    string Id,
    string Email,
    string DisplayName,
    DateTime CreatedAt
);
