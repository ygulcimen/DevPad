namespace DevPad.Models;

public enum JwtExpiryStatus { Valid, Expired, NotYetValid, NoExpiry }

/// <summary>Decoded JWT with all parts parsed. Immutable.</summary>
public record JwtToken(
    string RawHeader,
    string RawPayload,
    string RawSignature,
    string FormattedHeader,
    string FormattedPayload,
    string Algorithm,
    string TokenType,
    JwtExpiryStatus ExpiryStatus,
    string ExpiryMessage
);
