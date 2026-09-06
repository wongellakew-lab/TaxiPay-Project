namespace TaxiPayApi.Application.Common.Configuration;

public class CacheSettings
{
    public const string SectionName = "Cache";

    public int DefaultExpirationMinutes { get; set; } = 5;
    public int RouteExpirationMinutes { get; set; } = 15;
    public int FareMatrixExpirationMinutes { get; set; } = 15;
    public int DriverLookupExpirationMinutes { get; set; } = 5;
}
