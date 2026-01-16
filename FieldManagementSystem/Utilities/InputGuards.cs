using System.Net.Mail;

namespace FieldManagementSystem.Utilities;

public static class InputGuards
{
    public static string NormalizeEmail(string email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();

    public static void ValidateEmailOrThrow(string email)
    {
        email = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        try
        {
            var addr = new MailAddress(email);
            // Basic sanity check
            if (!addr.Address.Equals(email, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid email format.", nameof(email));
        }
        catch
        {
            throw new ArgumentException("Invalid email format.", nameof(email));
        }
    }

    public static void ValidateNameOrThrow(string value, string fieldName, int maxLen = 100)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{fieldName} is required.", nameof(value));

        if (value.Length > maxLen)
            throw new ArgumentException($"{fieldName} cannot exceed {maxLen} characters.", nameof(value));
    }
}