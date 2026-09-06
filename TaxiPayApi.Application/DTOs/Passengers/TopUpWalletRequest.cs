namespace TaxiPayApi.Application.DTOs.Passengers;

public record TopUpWalletRequest
{
    public decimal Amount { get; init; }
    public string? PaymentMethod { get; init; }
}
