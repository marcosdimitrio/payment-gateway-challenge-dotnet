using System.Collections;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Tests
{
    public class RejectedPaymentTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            // CardNumber
            yield return GetPayment(x => x.CardNumber = string.Empty, "The card number is required.");
            yield return GetPayment(x => x.CardNumber = "1111222233334", "The card number must be between 14 and 19 characters in length.");
            yield return GetPayment(x => x.CardNumber = "11112222333344445555", "The card number must be between 14 and 19 characters in length.");
            yield return GetPayment(x => x.CardNumber = "1111222233334444a", "The card number must only contain numeric characters.");
            yield return GetPayment(x => x.CardNumber = "1111222233334444 ", "The card number must only contain numeric characters.");
            yield return GetPayment(x => x.CardNumber = " 1111222233334444", "The card number must only contain numeric characters.");

            // ExpiryMonth
            yield return GetPayment(x => x.ExpiryMonth = 0, "Expiry month must be between 1-12.");
            yield return GetPayment(x => x.ExpiryMonth = 13, "Expiry month must be between 1-12.");

            // ExpiryYear
            yield return GetPayment(x =>
            {
                x.ExpiryMonth = 1;
                x.ExpiryYear = 1900;
            }, "The expiry month/year combination must be in the future.");

            // Currency
            yield return GetPayment(x => x.Currency = "GB", "The currency code must be exactly 3 characters.");
            yield return GetPayment(x => x.Currency = "GBPP", "The currency code must be exactly 3 characters.");
            yield return GetPayment(x => x.Currency = "GBP ", "The currency code must be exactly 3 characters.");
            yield return GetPayment(x => x.Currency = "ABC", "The currency code is not a valid ISO currency code.");

            // Amount
            yield return GetPayment(x => x.Amount = 0, "The amount must be greater than zero.");
            yield return GetPayment(x => x.Amount = -1, "The amount must be greater than zero.");

            // Cvv
            yield return GetPayment(x => x.Cvv = string.Empty, "The CVV is required.");
            yield return GetPayment(x => x.Cvv = "12", "The CVV must be between 3 and 4 characters in length.");
            yield return GetPayment(x => x.Cvv = "1234 ", "The CVV must be between 3 and 4 characters in length.");
            yield return GetPayment(x => x.Cvv = "12345", "The CVV must be between 3 and 4 characters in length.");
            yield return GetPayment(x => x.Cvv = "12a", "The CVV must only contain numeric characters.");
            yield return GetPayment(x => x.Cvv = "12 ", "The CVV must only contain numeric characters.");
            yield return GetPayment(x => x.Cvv = "123 ", "The CVV must only contain numeric characters.");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class TestData
        {
            public required PostPaymentRequest PostPaymentRequest { get; set; }
            public required string Message { get; set; }

            public override string ToString()
            {
                return Message;
            }
        }

        private object[] GetPayment(Action<PostPaymentRequest> changeData, string message)
        {
            var paymentRequest = new PostPaymentRequest()
            {
                CardNumber = "4444333322221111",
                ExpiryMonth = 1,
                ExpiryYear = TimeProvider.System.GetUtcNow().Year + 2,
                Currency = "GBP",
                Amount = 100,
                Cvv = "123",
            };

            changeData(paymentRequest);

            return [new TestData() { PostPaymentRequest = paymentRequest, Message = message }];
        }
    }
}
