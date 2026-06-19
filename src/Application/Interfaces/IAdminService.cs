using Application.Common;
using Application.DTOs.Auth;
using Domain.Enums;

namespace Application.Interfaces;

public interface IAdminService
{
    OperationResult<AuthResult> BootstrapAdmin(RegisterRequest request);
    OperationResult<AuthResult> CreateOperator(Guid adminUserId, RegisterRequest request);
    OperationResult SetSlotStatus(Guid adminUserId, string slotCode, SlotStatus status);
}
