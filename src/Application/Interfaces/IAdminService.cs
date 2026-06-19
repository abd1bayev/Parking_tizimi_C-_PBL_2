using Application.Common;
using Application.DTOs.Auth;

namespace Application.Interfaces;

public interface IAdminService
{
    OperationResult<AuthResult> BootstrapAdmin(RegisterRequest request);
    OperationResult<AuthResult> CreateOperator(Guid adminUserId, RegisterRequest request);
}
