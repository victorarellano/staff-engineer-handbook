using RateLimitedServiceSimulation.Api;
using RateLimitedServiceSimulation.Domain;

namespace RateLimitedServiceSimulation.Naive;
public sealed class PaymentService
{
    private readonly PaymentApi _paymentApi;

    public PaymentService(PaymentApi paymentApi)
    {
        _paymentApi = paymentApi;
    }

    public Task<bool> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        return _paymentApi.ProcessAsync(request, cancellationToken);
    }
}