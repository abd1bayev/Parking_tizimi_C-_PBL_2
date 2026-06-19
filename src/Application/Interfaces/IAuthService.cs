using Application.DTOs.Auth;
using Application.Common;

namespace Application.Interfaces;

public interface IAuthService
{
    bool HasAdmin();
    OperationResult<AuthResult> Login(LoginRequest request);
    OperationResult<AuthResult> Register(RegisterRequest request);
}
