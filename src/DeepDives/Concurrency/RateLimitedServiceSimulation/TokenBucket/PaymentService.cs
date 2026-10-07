using System.ComponentModel;
using System.Numerics;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using RateLimitedServiceSimulation.Api;
using RateLimitedServiceSimulation.Configuration;
using RateLimitedServiceSimulation.Domain;

namespace RateLimitedServiceSimulation.TokenBucket;

public sealed class PaymentService
{
    RateLimitedServiceSettings _settings;
    private readonly PaymentApi _paymentApi;
    private readonly TokenBucketRateLimiter _rateLimiter;

    public PaymentService(PaymentApi paymentApi, IOptions<RateLimitedServiceSettings> options)
    {
        _paymentApi = paymentApi;

        _settings = options.Value;

        var effectiveRate = _settings.ExternalLimitPerSecond * _settings.SafetyMargin;
        var replenishmentPeriod = TimeSpan.FromMilliseconds((double)(1000m / effectiveRate));

        _rateLimiter = new TokenBucketRateLimiter(
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1,
                TokensPerPeriod = 1,
                ReplenishmentPeriod = replenishmentPeriod,
                QueueLimit = _settings.RequestCount,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
    }

    public async Task<bool> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _settings.MaxAttempts; attempt++)
        {
            using var lease = await _rateLimiter.AcquireAsync(permitCount: 1, cancellationToken);

            if (!lease.IsAcquired)
            {
                return false;
            }

            var success = await _paymentApi.ProcessAsync(request, cancellationToken);
            if (success)
                return true;

            if (attempt < _settings.MaxAttempts)
            {
                await Task.Delay(250, cancellationToken);
            }        
        }

        return false;
    }
}