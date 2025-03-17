using FluentValidation;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validators
{
    public sealed class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
    {
        private static readonly string[] IsoCurrencyCodes = ["USD", "EUR", "GBP"];

        public PostPaymentRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(x => x.CardNumber)
                .NotEmpty()
                .WithMessage("The card number is required.")
                .Length(14, 19)
                .WithMessage("The card number must be between 14 and 19 characters in length.")
                .Must(x => x.All(c => c >= '0' && c <= '9'))
                .WithMessage("The card number must only contain numeric characters.");

            RuleFor(x => x.ExpiryMonth)
                .InclusiveBetween(1, 12)
                .WithMessage("Expiry month must be between 1-12.");

            var utcNow = timeProvider.GetUtcNow();

            RuleFor(x => x)
                .Must(x =>
                {
                    try
                    {
                        return new DateTime(x.ExpiryYear, x.ExpiryMonth, 1) > new DateTime(utcNow.Year, utcNow.Month, 1);
                    }
                    catch { return false; }
                })
                .WithName("Expiry")
                .WithMessage("The expiry month/year combination must be in the future.");

            RuleFor(x => x.Currency)
                .Length(3)
                .WithMessage("The currency code must be exactly 3 characters.")
                .Must(x => IsoCurrencyCodes.Contains(x, StringComparer.InvariantCultureIgnoreCase))
                .WithMessage("The currency code is not a valid ISO currency code.");

            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("The amount must be greater than zero.");

            RuleFor(x => x.Cvv)
                .NotEmpty()
                .WithMessage("The CVV is required.")
                .Length(3, 4)
                .WithMessage("The CVV must be between 3 and 4 characters in length.")
                .Must(x => x.All(c => c >= '0' && c <= '9'))
                .WithMessage("The CVV must only contain numeric characters.");
        }
    }
}
