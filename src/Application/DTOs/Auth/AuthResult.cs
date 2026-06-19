using Domain.Entities;
using Domain.Enums;

namespace Application.DTOs.Auth;

public sealed class AuthResult
{
    public Guid UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public string Message { get; init; } = string.Empty;

    public static AuthResult FromUser(User user, string message) => new()
    {
        UserId = user.Id,
        Username = user.Username,
        Role = user.Role,
        Message = message
    };
}
