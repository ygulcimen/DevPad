using System;
using System.Text;

namespace DevPad.Services;

public class Base64Service
{
    /// <summary>
    /// Encodes UTF-8 text to Base64 (standard or URL-safe).
    /// URL-safe replaces +→- and /→_ and strips padding.
    /// </summary>
    public string Encode(string text, bool urlSafe)
    {
        var bytes  = Encoding.UTF8.GetBytes(text);
        var result = Convert.ToBase64String(bytes);
        if (urlSafe)
            result = result.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return result;
    }

    /// <summary>
    /// Decodes a Base64 string back to UTF-8 text.
    /// Accepts both standard and URL-safe variants regardless of the urlSafe flag.
    /// Returns null result and a message on failure.
    /// </summary>
    public (string? Result, string? Error) Decode(string base64, bool urlSafe)
    {
        try
        {
            // Normalise whichever variant arrives
            var normalised = base64.Trim()
                .Replace('-', '+')
                .Replace('_', '/');

            // Re-add padding stripped by URL-safe encoding
            switch (normalised.Length % 4)
            {
                case 2: normalised += "=="; break;
                case 3: normalised += "=";  break;
            }

            var bytes = Convert.FromBase64String(normalised);
            return (Encoding.UTF8.GetString(bytes), null);
        }
        catch (FormatException)
        {
            return (null, "Invalid Base64 input — unexpected characters or incorrect length");
        }
        catch (Exception ex)
        {
            return (null, ex.Message);
        }
    }
}
