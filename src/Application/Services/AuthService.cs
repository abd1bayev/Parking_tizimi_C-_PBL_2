using Application.Common;
using Application.DTOs.Auth;
using Application.Interfaces;
using Application.Internal;
using Domain.Enums;

namespace Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IParkingStateStore _stateStore;

    public AuthService(IParkingStateStore stateStore, IPasswordHasher passwordHasher, IClock clock)
    {
        _stateStore = stateStore;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public bool HasAdmin() =>
        _stateStore.State.Users.Any(user => user.Role == UserRole.Admin && user.IsActive);

    public OperationResult<AuthResult> Login(LoginRequest request)
    {
        if (!InputNormalizer.TryNormalizeRequired(request.Username, out var username))
        {
            return OperationResult<AuthResult>.Failure("Foydalanuvchi nomi bo'sh bo'lmasligi kerak.");
        }

        var user = _stateStore.State.Users.FirstOrDefault(candidate =>
            string.Equals(candidate.Username, username, StringComparison.OrdinalIgnoreCase) && candidate.IsActive);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return OperationResult<AuthResult>.Failure("Kirish nomi yoki parol noto'g'ri.");
        }

        return OperationResult<AuthResult>.Success(
            AuthResult.FromUser(user, "Muvaffaqiyatli kirdingiz."));
    }

    public OperationResult<AuthResult> Register(RegisterRequest request)
    {
        if (!HasAdmin())
        {
            return OperationResult<AuthResult>.Failure(
                "Avval ma'mur yaratilishi kerak. Ma'mur tizimni boshlaydi.");
        }

        var result = UserRegistration.CreateUser(
            _stateStore.State,
            _passwordHasher,
            _clock,
            request.Username,
            request.Password,
            request.PhoneNumber,
            UserRole.User,
            "Foydalanuvchi yaratildi.");

        return result.Succeeded
            ? OperationResult<AuthResult>.Success(AuthResult.FromUser(result.Value!, result.Message))
            : OperationResult<AuthResult>.Failure(result.Message);
    }
}
