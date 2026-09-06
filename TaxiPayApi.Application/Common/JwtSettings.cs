namespace TaxiPayApi.Application.Common;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryHours { get; set; } = 8;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}