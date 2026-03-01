using System;
using System.Text;
using DevPad.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevPad.Services;

/// <summary>
/// Decodes JWTs offline using only Base64Url + JSON parsing.
/// No network calls are made — not even indirectly through any library.
/// </summary>
public class JwtDecoderService
{
    public (JwtToken? Token, string? Error) Decode(string jwt)
    {
        var trimmed = jwt.Trim();
        var parts = trimmed.Split('.');

        if (parts.Length != 3)
            return (null, $"Expected 3 dot-separated parts, found {parts.Length}");

        string headerJson, payloadJson;
        try
        {
            headerJson  = DecodeBase64Url(parts[0]);
            payloadJson = DecodeBase64Url(parts[1]);
        }
        catch (Exception ex)
        {
            return (null, $"Base64Url decode failed: {ex.Message}");
        }

        string formattedHeader, formattedPayload;
        string algorithm = "Unknown", tokenType = "JWT";

        try
        {
            var headerObj  = JObject.Parse(headerJson);
            formattedHeader = headerObj.ToString(Formatting.Indented);
            algorithm       = headerObj["alg"]?.ToString() ?? "Unknown";
            tokenType       = headerObj["typ"]?.ToString() ?? "JWT";
        }
        catch
        {
            formattedHeader = headerJson;
        }

        try
        {
            formattedPayload = JObject.Parse(payloadJson).ToString(Formatting.Indented);
        }
        catch
        {
            formattedPayload = payloadJson;
        }

        var (status, message) = GetExpiryInfo(payloadJson);

        var token = new JwtToken(
            RawHeader:       parts[0],
            RawPayload:      parts[1],
            RawSignature:    parts[2],
            FormattedHeader: formattedHeader,
            FormattedPayload: formattedPayload,
            Algorithm:       algorithm,
            TokenType:       tokenType,
            ExpiryStatus:    status,
            ExpiryMessage:   message
        );

        return (token, null);
    }

    /// <summary>
    /// Decodes a Base64Url-encoded string to UTF-8 text.
    /// This is pure in-process computation — no network activity.
    /// </summary>
    private static string DecodeBase64Url(string base64Url)
    {
        var padded = base64Url.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "=";  break;
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(padded));
    }

    private static (JwtExpiryStatus Status, string Message) GetExpiryInfo(string payloadJson)
    {
        try
        {
            var payload = JObject.Parse(payloadJson);

            // nbf = not before
            if (payload["nbf"] is { Type: JTokenType.Integer } nbfToken)
            {
                var nbf = DateTimeOffset.FromUnixTimeSeconds(nbfToken.Value<long>());
                if (nbf > DateTimeOffset.UtcNow)
                    return (JwtExpiryStatus.NotYetValid,
                        $"Not yet valid — activates in {FormatDuration(nbf - DateTimeOffset.UtcNow)}");
            }

            // exp = expires at
            if (payload["exp"] is { Type: JTokenType.Integer } expToken)
            {
                var exp = DateTimeOffset.FromUnixTimeSeconds(expToken.Value<long>());
                var now = DateTimeOffset.UtcNow;

                if (exp < now)
                    return (JwtExpiryStatus.Expired,
                        $"Expired {FormatDuration(now - exp)} ago  (at {exp:yyyy-MM-dd HH:mm:ss} UTC)");

                return (JwtExpiryStatus.Valid,
                    $"Valid — expires in {FormatDuration(exp - now)}  (at {exp:yyyy-MM-dd HH:mm:ss} UTC)");
            }

            return (JwtExpiryStatus.NoExpiry, "No expiry (exp) claim present");
        }
        catch
        {
            return (JwtExpiryStatus.NoExpiry, "Could not read expiry claims");
        }
    }

    private static string FormatDuration(TimeSpan span)
    {
        if (span.TotalDays  >= 1) return $"{(int)span.TotalDays}d {span.Hours}h";
        if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h {span.Minutes}m";
        if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes}m {span.Seconds}s";
        return $"{(int)span.TotalSeconds}s";
    }
}
