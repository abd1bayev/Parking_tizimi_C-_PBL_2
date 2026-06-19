using System.Text.RegularExpressions;

namespace ParkingTizimi.Shared.Validation;

public static partial class PhoneNumberValidator
{
    public static bool IsValid(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        return UzbekistanPhoneRegex().IsMatch(Normalize(phoneNumber));
    }

    public static string Normalize(string phoneNumber) => Regex.Replace(phoneNumber, @"\s+", string.Empty).Trim();

    [GeneratedRegex(@"^\+998\d{9}$", RegexOptions.Compiled)]
    private static partial Regex UzbekistanPhoneRegex();
}