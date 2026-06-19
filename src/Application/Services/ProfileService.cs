using Application.Common;
using Application.DTOs.Profile;
using Application.Interfaces;
using Application.Internal;
using Domain.Entities;

namespace Application.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IParkingStateStore _stateStore;

    public ProfileService(IParkingStateStore stateStore, IPasswordHasher passwordHasher)
    {
        _stateStore = stateStore;
        _passwordHasher = passwordHasher;
    }

    public OperationResult<User> GetProfile(Guid userId)
    {
        var user = ParkingStateHelper.GetActiveUser(_stateStore.State, userId);
        return user is null
            ? OperationResult<User>.Failure("Foydalanuvchi topilmadi.")
            : OperationResult<User>.Success(user, "Profil yuklandi.");
    }

    public OperationResult<User> UpdateProfile(UpdateProfileRequest request)
    {
        var user = ParkingStateHelper.GetActiveUser(_stateStore.State, request.UserId);
        if (user is null)
        {
            return OperationResult<User>.Failure("Foydalanuvchi topilmadi.");
        }

        if (!PhoneNumberValidator.IsValid(request.PhoneNumber))
        {
            return OperationResult<User>.Failure("Telefon raqam +998XXXXXXXXX formatida bo'lishi kerak.");
        }

        user.PhoneNumber = PhoneNumberValidator.Normalize(request.PhoneNumber);
        return OperationResult<User>.Success(user, "Profil yangilandi.");
    }

    public OperationResult ChangePassword(ChangePasswordRequest request)
    {
        var user = ParkingStateHelper.GetActiveUser(_stateStore.State, request.UserId);
        if (user is null)
        {
            return OperationResult.Failure("Foydalanuvchi topilmadi.");
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return OperationResult.Failure("Joriy parol noto'g'ri.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Trim().Length < 6)
        {
            return OperationResult.Failure("Yangi parol kamida 6 ta belgidan iborat bo'lishi kerak.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        return OperationResult.Success("Parol muvaffaqiyatli o'zgartirildi.");
    }
}
