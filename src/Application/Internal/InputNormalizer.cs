namespace Application.Internal;

internal static class InputNormalizer
{
    public static bool TryNormalizeRequired(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        normalized = value.Trim();
        return true;
    }
}
