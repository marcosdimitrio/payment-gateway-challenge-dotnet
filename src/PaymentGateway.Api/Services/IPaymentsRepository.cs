﻿
using PaymentGateway.Api.Entities;

namespace PaymentGateway.Api.Services
{
    public interface IPaymentsRepository
    {
        void Add(Payment payment);
        Payment? Get(Guid id);
    }
}