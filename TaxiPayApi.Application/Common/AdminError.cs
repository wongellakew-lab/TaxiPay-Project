namespace TaxiPayApi.Application.Common;

public sealed record AdminError(string Code, string Message)
{
    public static AdminError InvalidCredentials() =>
        new("invalid_credentials", "Invalid administrative credentials or inactive account.");

    public static AdminError AccountLockedOut(int remainingMinutes) =>
        new("account_locked_out", $"Administrative account is temporarily locked due to multiple failed login attempts. Please try again in {remainingMinutes} minute(s).");
}
