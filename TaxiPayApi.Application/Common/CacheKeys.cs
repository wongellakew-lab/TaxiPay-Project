namespace TaxiPayApi.Application.Common;

public static class CacheKeys
{
    // Key Prefixes & Templates
    public const string RoutesActive = "taxipay:routes:active";
    public const string RoutesAll = "taxipay:routes:all";

    public static string RouteById(Guid id) => $"taxipay:routes:{id}";
    public static string FaresForRoute(Guid routeId) => $"taxipay:fares:route:{routeId}";
    public static string Fare(Guid routeId, string fromNode, string toNode) =>
        $"taxipay:fares:{routeId}:{fromNode.Trim().ToLowerInvariant()}:{toNode.Trim().ToLowerInvariant()}";
    public static string DriverByCode(string driverCode) =>
        $"taxipay:drivers:code:{driverCode.Trim().ToLowerInvariant().Replace("-", "").Replace(" ", "").Replace("_", "")}";

    // Cache Tags for Targeted Group Invalidation
    public const string TagRoutes = "routes";
    public const string TagFares = "fares";
    public const string TagDrivers = "drivers";
}
