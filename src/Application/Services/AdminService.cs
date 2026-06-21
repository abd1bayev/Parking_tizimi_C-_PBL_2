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
            return OperationResult<AuthResult>.Failure("Ma'mur allaqachon mavjud.");
        }

        var result = UserRegistration.CreateUser(
            _stateStore.State,
            _passwordHasher,
            _clock,
            request.Username,
            request.Password,
            request.PhoneNumber,
            UserRole.Admin,
            "Ma'mur yaratildi.");

        return result.Succeeded
            ? OperationResult<AuthResult>.Success(AuthResult.FromUser(result.Value!, result.Message))
            : OperationResult<AuthResult>.Failure(result.Message);
    }

    public OperationResult<AuthResult> CreateOperator(Guid adminUserId, RegisterRequest request)
    {
        if (!ParkingStateHelper.IsAdmin(_stateStore.State, adminUserId))
        {
            return OperationResult<AuthResult>.Failure("Faqat ma'mur operator yarata oladi.");
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

    public OperationResult SetSlotStatus(Guid adminUserId, string slotCode, SlotStatus status)
    {
        if (!ParkingStateHelper.IsAdmin(_stateStore.State, adminUserId))
        {
            return OperationResult.Failure("Faqat ma'mur park joy holatini o'zgartira oladi.");
        }

        if (status is not (SlotStatus.Available or SlotStatus.OutOfService))
        {
            return OperationResult.Failure("Faqat «Bo'sh» yoki «Ta'mirda» holatlari qo'llab-quvvatlanadi.");
        }

        var slot = ParkingStateHelper.GetSlotByCode(_stateStore.State, slotCode);
        if (slot is null)
        {
            return OperationResult.Failure("Park joyi topilmadi.");
        }

        if (status == SlotStatus.OutOfService &&
            _stateStore.State.Sessions.Any(session => session.SlotId == slot.Id && !session.IsClosed))
        {
            return OperationResult.Failure("Band park joyini ta'mir holatiga o'tkazib bo'lmaydi.");
        }

        slot.Status = status;
        if (status == SlotStatus.Available)
        {
            ParkingStateHelper.RecalculateSlotStatus(_stateStore.State, slot.Id);
        }

        return OperationResult.Success($"Park joyi {slot.Code} holati yangilandi: {UiLabels.FormatSlotStatus(status)}.");
    }
}
