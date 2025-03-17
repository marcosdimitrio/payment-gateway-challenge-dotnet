using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Services.Dto
{
    public sealed class AcquiringBankResponse
    {
        [JsonPropertyName("authorized")]
        public bool Authorized { get; set; }

        [JsonPropertyName("authorization_code")]
        public string AuthorizationCode { get; set; } = string.Empty;
    }
}
