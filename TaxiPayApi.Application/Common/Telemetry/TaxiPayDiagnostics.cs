using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TaxiPayApi.Application.Common.Telemetry;

public static class TaxiPayDiagnostics
{
    public const string ServiceName = "TaxiPay.Api";
    public const string ServiceVersion = "1.0.0";

    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);
    public static readonly Meter Meter = new(ServiceName, ServiceVersion);

    // Business & Operational Metrics
    public static readonly Counter<long> PaymentsTotal = Meter.CreateCounter<long>(
        "taxipay.payments.total",
        unit: "{payments}",
        description: "Total number of payment transactions processed");

    public static readonly Histogram<double> PaymentsAmount = Meter.CreateHistogram<double>(
        "taxipay.payments.amount",
        unit: "ETB",
        description: "Monetary amount of completed payment transactions");

    public static readonly Counter<long> TripsTotal = Meter.CreateCounter<long>(
        "taxipay.trips.total",
        unit: "{trips}",
        description: "Total number of trip lifecycle events (start, complete, alight)");

    public static readonly Counter<long> CacheHitsTotal = Meter.CreateCounter<long>(
        "taxipay.cache.hits.total",
        unit: "{hits}",
        description: "Total number of HybridCache hits");

    public static readonly Counter<long> CacheMissesTotal = Meter.CreateCounter<long>(
        "taxipay.cache.misses.total",
        unit: "{misses}",
        description: "Total number of HybridCache misses");

    public static readonly Counter<long> RateLimitRejectedTotal = Meter.CreateCounter<long>(
        "taxipay.ratelimit.rejected.total",
        unit: "{requests}",
        description: "Total number of requests rejected by the rate limiter");

    public static readonly Counter<long> DisputesTotal = Meter.CreateCounter<long>(
        "taxipay.disputes.total",
        unit: "{disputes}",
        description: "Total number of dispute lifecycle operations");
}
