using FluentValidation;

using Microsoft.Extensions.Options;

using PaymentGateway.Api;
using PaymentGateway.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AcquirerBankOptions>(
    builder.Configuration.GetSection(nameof(AcquirerBankOptions)));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<PaymentsService>();

builder.Services.AddHttpClient("acquiringBank", (serviceProvider, httpClient) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<AcquirerBankOptions>>().Value;

    httpClient.BaseAddress = new Uri(settings.BaseAddress);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler("/error");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
