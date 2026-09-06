using System.Text.Json.Serialization;

namespace TaxiPayApi.Application.DTOs.Drivers;

public record RequestRouteChangeRequest
{
    [JsonPropertyName("newRouteId")]
    public Guid NewRouteId { get; init; }

    [JsonPropertyName("requestedRouteId")]
    public Guid? RequestedRouteId { get; init; }

    [JsonIgnore]
    public Guid EffectiveNewRouteId => RequestedRouteId.HasValue && RequestedRouteId.Value != Guid.Empty
        ? RequestedRouteId.Value
        : NewRouteId;

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;
}