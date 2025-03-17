﻿using PaymentGateway.Api.Entities;

namespace PaymentGateway.Api.Services;

public sealed class PaymentsRepository : IPaymentsRepository
{
    private readonly List<Payment> _payments = new();

    public void Add(Payment payment)
    {
        _payments.Add(payment);
    }

    public Payment? Get(Guid id)
    {
        return _payments.FirstOrDefault(p => p.Id == id);
    }
}