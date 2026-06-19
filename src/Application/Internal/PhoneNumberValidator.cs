using System.Text.RegularExpressions;

namespace Application.Internal;

internal static partial class PhoneNumberValidator
{
    private static readonly Regex Pattern = PhoneRegex();

    public static bool IsValid(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        return Pattern.IsMatch(phoneNumber.Trim());
    }

    public static string Normalize(string phoneNumber) => phoneNumber.Trim();

    [GeneratedRegex(@"^\+998\d{9}$")]
    private static partial Regex PhoneRegex();
}
