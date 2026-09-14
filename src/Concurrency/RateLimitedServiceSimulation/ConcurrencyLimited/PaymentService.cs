using Microsoft.Extensions.Options;
using RateLimitedServiceSimulation.Api;
using RateLimitedServiceSimulation.Configuration;
using RateLimitedServiceSimulation.Domain;

namespace RateLimitedServiceSimulation.ConcurrenceLimited;

public sealed class PaymentService
{
    private readonly RateLimitedServiceSettings _settings;
    private readonly PaymentApi _paymentApi;
    private readonly SemaphoreSlim _concurrencyLimit;

    public PaymentService(PaymentApi paymentApi, IOptions<RateLimitedServiceSettings> options)
    {
        _settings = options.Value;
        
        _paymentApi = paymentApi;
        _concurrencyLimit = new SemaphoreSlim(_settings.ExternalLimitPerSecond, _settings.ExternalLimitPerSecond);
    }

    public async Task<bool> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        await _concurrencyLimit.WaitAsync(cancellationToken);

        try
        {
            return await _paymentApi.ProcessAsync(request, cancellationToken);
        }
        finally
        {
            _concurrencyLimit.Release();
        }
    }
}