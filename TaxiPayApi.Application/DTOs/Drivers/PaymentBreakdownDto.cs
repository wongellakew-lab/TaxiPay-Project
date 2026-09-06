namespace TaxiPayApi.Application.DTOs.Drivers;

public record PaymentBreakdownDto
{
    public decimal Digital { get; init; }
    public decimal Cash { get; init; }
    public decimal Telebirr { get; init; }
    public decimal Wallet { get; init; }
}