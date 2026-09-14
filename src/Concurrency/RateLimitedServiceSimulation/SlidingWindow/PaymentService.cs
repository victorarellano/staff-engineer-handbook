using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using RateLimitedServiceSimulation.Api;
using RateLimitedServiceSimulation.Configuration;
using RateLimitedServiceSimulation.Domain;

namespace RateLimitedServiceSimulation.SlidingWindow;

public sealed class PaymentService
{
    private readonly PaymentApi _paymentApi;
    private readonly SlidingWindowRateLimiter _rateLimiter;

    public PaymentService(PaymentApi paymentApi, IOptions<RateLimitedServiceSettings> options)
    {
        _paymentApi = paymentApi;

        var settings = options.Value;

        _rateLimiter = new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions
            {
                PermitLimit = settings.ExternalLimitPerSecond,
                Window = TimeSpan.FromSeconds(1),
                SegmentsPerWindow = 10,
                QueueLimit = settings.RequestCount,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    public async Task<bool> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);

        if (!lease.IsAcquired)
        {
            return false;
        }

        return await _paymentApi.ProcessAsync(request, cancellationToken);
    }
}