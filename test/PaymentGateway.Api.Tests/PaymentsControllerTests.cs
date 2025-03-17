using System.Net;
using System.Net.Http.Json;

using FakeItEasy;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Entities;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();

    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999).ToString(),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var client = CreateHttpClient(paymentsRepository);

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<GetPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.True(paymentResponse!.CardNumberLastFour.Length <= 4);

        AssertExpectedFieldsAreReturnedInGetResponse(payment, paymentResponse!);
    }

    [Fact]
    public async Task MakesAnAuthorizedPayment()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var client = CreateHttpClient(paymentsRepository);

        var paymentRequest = new PostPaymentRequest()
        {
            CardNumber = "4444333322221111", // ending on odd number for authorized response from simulator
            ExpiryMonth = 1,
            ExpiryYear = TimeProvider.System.GetUtcNow().Year + 2,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123",
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments/", paymentRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Authorized, paymentResponse!.Status);
        Assert.True(paymentResponse!.CardNumberLastFour.Length <= 4);

        AssertPaymentWasSavedToDatabase(paymentsRepository, paymentResponse);
        AssertExpectedFieldsAreReturnedInPostResponse(paymentRequest, paymentResponse);
    }

    [Fact]
    public async Task DeclinesAPaymentWhenBankDoesNotAuthorize()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var client = CreateHttpClient(paymentsRepository);

        var paymentRequest = new PostPaymentRequest()
        {
            CardNumber = "4444333322221112", // ending on even number for unauthorized response from simulator
            ExpiryMonth = 1,
            ExpiryYear = TimeProvider.System.GetUtcNow().Year + 2,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123",
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments/", paymentRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Declined, paymentResponse!.Status);
        Assert.True(paymentResponse!.CardNumberLastFour.Length <= 4);

        AssertPaymentWasSavedToDatabase(paymentsRepository, paymentResponse);
    }

    [Fact]
    public async Task DeclinesAPaymentWhenBankIsUnavailable()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var client = CreateHttpClient(paymentsRepository);

        var paymentRequest = new PostPaymentRequest()
        {
            CardNumber = "4444333322221110", // ending on zero for 503 error from simulator
            ExpiryMonth = 1,
            ExpiryYear = TimeProvider.System.GetUtcNow().Year + 2,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123",
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments/", paymentRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Declined, paymentResponse!.Status);
        Assert.True(paymentResponse!.CardNumberLastFour.Length <= 4);

        AssertPaymentWasSavedToDatabase(paymentsRepository, paymentResponse);
    }

    [Theory]
    [ClassData(typeof(RejectedPaymentTestData))]
    public async Task RejectsAPayment(RejectedPaymentTestData.TestData testData)
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        var client = CreateHttpClient(paymentsRepository);

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments/", testData.PostPaymentRequest);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Rejected, paymentResponse!.Status);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsInternalServerErrorWhenGetThrowsAnException()
    {
        // Arrange
        var paymentsRepository = A.Fake<IPaymentsRepository>();
        A.CallTo(() => paymentsRepository.Get(A<Guid>._)).Throws(new Exception("Test exception"));

        var client = CreateHttpClient(paymentsRepository);

        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal("An error occurred while processing your request.", problemDetails!.Title);
    }

    [Fact]
    public async Task ReturnsInternalServerErrorWhenPostThrowsAnException()
    {
        // Arrange
        var paymentRequest = new PostPaymentRequest()
        {
            CardNumber = "4444333322221111",
            ExpiryMonth = 1,
            ExpiryYear = TimeProvider.System.GetUtcNow().Year + 2,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123",
        };

        var paymentsRepository = A.Fake<IPaymentsRepository>();
        A.CallTo(() => paymentsRepository.Add(A<Payment>._)).Throws(new Exception("Test exception"));

        var client = CreateHttpClient(paymentsRepository);

        // Act
        var response = await client.PostAsJsonAsync($"/api/Payments/", paymentRequest);
        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(problemDetails);
        Assert.Equal("An error occurred while processing your request.", problemDetails!.Title);
    }

    private static void AssertPaymentWasSavedToDatabase(PaymentsRepository paymentsRepository, PostPaymentResponse paymentResponse)
    {
        var payment = paymentsRepository.Get(paymentResponse!.Id);
        Assert.NotNull(payment);
        Assert.Equal(paymentResponse.Id, payment!.Id);
        Assert.Equal(paymentResponse.Status, payment!.Status);
        Assert.Equal(paymentResponse.CardNumberLastFour, payment!.CardNumberLastFour);
        Assert.Equal(paymentResponse.ExpiryMonth, payment!.ExpiryMonth);
        Assert.Equal(paymentResponse.ExpiryYear, payment!.ExpiryYear);
        Assert.Equal(paymentResponse.Currency, payment!.Currency);
        Assert.Equal(paymentResponse.Amount, payment!.Amount);
    }

    private static void AssertExpectedFieldsAreReturnedInGetResponse(Payment payment, GetPaymentResponse paymentResponse)
    {
        Assert.NotEqual(Guid.Empty, paymentResponse.Id);
        Assert.Equal(payment.Id, paymentResponse.Id);
        Assert.Equal(payment.CardNumberLastFour, paymentResponse.CardNumberLastFour);
        Assert.Equal(payment.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(payment.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(payment.Currency, paymentResponse.Currency);
        Assert.Equal(payment.Amount, paymentResponse.Amount);
    }

    private static void AssertExpectedFieldsAreReturnedInPostResponse(PostPaymentRequest paymentRequest, PostPaymentResponse paymentResponse)
    {
        Assert.NotEqual(Guid.Empty, paymentResponse.Id);
        Assert.Equal(paymentRequest.CardNumber.Right(4), paymentResponse.CardNumberLastFour);
        Assert.Equal(paymentRequest.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(paymentRequest.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(paymentRequest.Currency, paymentResponse.Currency);
        Assert.Equal(paymentRequest.Amount, paymentResponse.Amount);
    }

    private static HttpClient CreateHttpClient(IPaymentsRepository paymentsRepository)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();
        return client;
    }
}