using System.Text.Json.Serialization;

using PaymentGateway.Api.Entities;

namespace PaymentGateway.Api.Models.Responses;

public class GetPaymentResponse
{
    public Guid Id { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus Status { get; set; }
    public string CardNumberLastFour { get; set; } = string.Empty;
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int Amount { get; set; }

    internal static GetPaymentResponse Map(Payment payment)
    {
        var newItem = new GetPaymentResponse()
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount,
        };

        return newItem;
    }
}