using Application.Common;
using Application.DTOs.Profile;
using Domain.Entities;

namespace Application.Interfaces;

public interface IProfileService
{
    OperationResult<User> GetProfile(Guid userId);
    OperationResult<User> UpdateProfile(UpdateProfileRequest request);
    OperationResult ChangePassword(ChangePasswordRequest request);
}
