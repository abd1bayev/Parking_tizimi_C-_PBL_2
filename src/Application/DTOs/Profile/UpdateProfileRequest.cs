namespace Application.DTOs.Profile;

public sealed class UpdateProfileRequest
{
    public Guid UserId { get; init; }
    public string PhoneNumber { get; init; } = string.Empty;
}
