namespace TaxiPayApi.Application.DTOs.Passengers;

public record TopUpWalletResponse
{
    public Guid PassengerId { get; init; }
    public decimal Amount { get; init; }
    public decimal NewBalance { get; init; }
    public string TransactionRef { get; init; } = string.Empty;
    public DateTime TopUpTime { get; init; }
}
