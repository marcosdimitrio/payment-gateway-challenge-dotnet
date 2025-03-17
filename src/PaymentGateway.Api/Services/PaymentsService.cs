using PaymentGateway.Api.Entities;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services.Dto;

namespace PaymentGateway.Api.Services
{
    public sealed class PaymentsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPaymentsRepository _paymentsRepository;

        public PaymentsService(IHttpClientFactory httpClientFactory, IPaymentsRepository paymentsRepository)
        {
            _httpClientFactory = httpClientFactory;
            _paymentsRepository = paymentsRepository;
        }

        public async Task<PostPaymentResponse> SendPayment(PostPaymentRequest request, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("acquiringBank");

            var acquiringBankRequest = MapAcquiringBankRequest(request);
            var content = await client.PostAsJsonAsync("payments", acquiringBankRequest, cancellationToken);
            var acquiringBankResponse = await content.Content.ReadFromJsonAsync<AcquiringBankResponse>(cancellationToken);

            var paymentStatus = GetPaymentStatus(content, acquiringBankResponse);
            var paymentResponse = MapPostPaymentResponse(request, paymentStatus);

            SaveSuccessfulPaymentToDatabase(paymentResponse);

            return paymentResponse;
        }

        private void SaveSuccessfulPaymentToDatabase(PostPaymentResponse paymentResponse)
        {
            var statusToSave = new[] { PaymentStatus.Authorized, PaymentStatus.Declined };

            if (statusToSave.Contains(paymentResponse.Status))
            {
                var payment = MapPayment(paymentResponse);
                _paymentsRepository.Add(payment);
            }
        }

        private static AcquiringBankRequest MapAcquiringBankRequest(PostPaymentRequest request)
        {
            var newItem = new AcquiringBankRequest()
            {
                CardNumber = request.CardNumber,
                ExpiryDate = $"{request.ExpiryMonth}/{request.ExpiryYear}",
                Currency = request.Currency,
                Amount = request.Amount,
                Cvv = $"{request.Cvv}",
            };

            return newItem;
        }

        private static PaymentStatus GetPaymentStatus(HttpResponseMessage content, AcquiringBankResponse? acquiringBankResponse)
        {
            if (content.IsSuccessStatusCode && acquiringBankResponse is not null && acquiringBankResponse.Authorized)
            {
                return PaymentStatus.Authorized;
            }

            return PaymentStatus.Declined;
        }

        private static PostPaymentResponse MapPostPaymentResponse(PostPaymentRequest request, PaymentStatus paymentStatus)
        {
            var newItem = new PostPaymentResponse()
            {
                Id = Guid.NewGuid(),
                Status = paymentStatus,
                CardNumberLastFour = request.CardNumber.Right(4),
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount,
            };

            return newItem;
        }

        private static Payment MapPayment(PostPaymentResponse paymentResponse)
        {
            var newItem = new Payment()
            {
                Id = paymentResponse.Id,
                Status = paymentResponse.Status,
                CardNumberLastFour = paymentResponse.CardNumberLastFour,
                ExpiryMonth = paymentResponse.ExpiryMonth,
                ExpiryYear = paymentResponse.ExpiryYear,
                Currency = paymentResponse.Currency,
                Amount = paymentResponse.Amount,
            };

            return newItem;
        }
    }
}
