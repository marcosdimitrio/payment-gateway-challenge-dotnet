using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsService _paymentsService;
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IValidator<PostPaymentRequest> _postPaymentRequestValidator;

    public PaymentsController(PaymentsService paymentsService, IPaymentsRepository paymentsRepository, IValidator<PostPaymentRequest> postPaymentRequestValidator)
    {
        _paymentsService = paymentsService;
        _paymentsRepository = paymentsRepository;
        _postPaymentRequestValidator = postPaymentRequestValidator;
    }

    [HttpGet("{id:guid}")]
    public ActionResult<GetPaymentResponse?> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment == null)
        {
            return NotFound();
        }

        var getPaymentResponse = GetPaymentResponse.Map(payment);

        return new OkObjectResult(getPaymentResponse);
    }

    [HttpPost("")]
    public async Task<ActionResult<PostPaymentResponse>> Post(PostPaymentRequest request, CancellationToken cancellationToken)
    {
        var validationResult = _postPaymentRequestValidator.Validate(request);

        if (!validationResult.IsValid)
        {
            var rejectedResponse = GetRejectedResponse(request);

            return BadRequest(rejectedResponse);
        }

        var response = await _paymentsService.SendPayment(request, cancellationToken);

        if (response.Status == PaymentStatus.Authorized)
        {
            return new OkObjectResult(response);
        }

        return UnprocessableEntity(response);
    }

    private static PostPaymentResponse GetRejectedResponse(PostPaymentRequest request)
    {
        var newItem = new PostPaymentResponse()
        {
            Status = PaymentStatus.Rejected,
            CardNumberLastFour = request.CardNumber.Right(4),
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount,
        };

        return newItem;
    }
}