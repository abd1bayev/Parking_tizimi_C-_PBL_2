using Application.Common;
using Application.DTOs.Auth;
using Application.Interfaces;
using Application.Internal;
using Domain.Enums;

namespace Application.Services;

public sealed class AdminService : IAdminService
{
    private readonly IClock _clock;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IParkingStateStore _stateStore;

    public AdminService(IParkingStateStore stateStore, IPasswordHasher passwordHasher, IClock clock)
    {
        _stateStore = stateStore;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public OperationResult<AuthResult> BootstrapAdmin(RegisterRequest request)
    {
        if (_stateStore.State.Users.Any(user => user.Role == UserRole.Admin))
        {
            return OperationResult<AuthResult>.Failure("Admin allaqachon mavjud.");
        }

        var result = UserRegistration.CreateUser(
            _stateStore.State,
            _passwordHasher,
            _clock,
            request.Username,
            request.Password,
            request.PhoneNumber,
            UserRole.Admin,
            "Admin yaratildi.");

        return result.Succeeded
            ? OperationResult<AuthResult>.Success(AuthResult.FromUser(result.Value!, result.Message))
            : OperationResult<AuthResult>.Failure(result.Message);
    }

    public OperationResult<AuthResult> CreateOperator(Guid adminUserId, RegisterRequest request)
    {
        if (!ParkingStateHelper.IsAdmin(_stateStore.State, adminUserId))
        {
            return OperationResult<AuthResult>.Failure("Faqat admin operator yarata oladi.");
        }

        var result = UserRegistration.CreateUser(
            _stateStore.State,
            _passwordHasher,
            _clock,
            request.Username,
            request.Password,
            request.PhoneNumber,
            UserRole.Operator,
            "Operator yaratildi.");

        return result.Succeeded
            ? OperationResult<AuthResult>.Success(AuthResult.FromUser(result.Value!, result.Message))
            : OperationResult<AuthResult>.Failure(result.Message);
    }
}
