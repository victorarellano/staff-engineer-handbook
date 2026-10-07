using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RateLimitedServiceSimulation.Configuration;
using RateLimitedServiceSimulation.Domain;

namespace RateLimitedServiceSimulation.Api;

public sealed class PaymentApi
{
    private readonly RateLimitedServiceSettings _settings;
    private readonly ILogger<PaymentApi> _logger;

    private readonly object _syncRoot = new();

    private DateTime _windowStart = DateTime.UtcNow;
    private int _requestCountInWindow;

    public PaymentApi(IOptions<RateLimitedServiceSettings> options, ILogger<PaymentApi> logger)
    {
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        var accepted = false;
        double elapsedMilliseconds;
        DateTime windowStart;
        int requestNumber;

        lock (_syncRoot)
        {
            var now = DateTime.UtcNow;
            var totalTime = now - _windowStart;

            elapsedMilliseconds = (now - _windowStart).TotalMilliseconds;

            if (now - _windowStart >= TimeSpan.FromSeconds(1))
            {
                _windowStart = now;
                _requestCountInWindow = 0;
                elapsedMilliseconds = 0;
            }

            if (_requestCountInWindow < _settings.ExternalLimitPerSecond)
            {
                _requestCountInWindow++;
                accepted = true;
            }

            windowStart = _windowStart;
            requestNumber = _requestCountInWindow;
        }

        if (!accepted)
        {
            _logger.LogWarning("Request {RequestId} rejected : 429 Too Many Requests", request.Id);

            return false;
        }

        _logger.LogInformation(
            "Request {RequestId} accepted after {Elapsed:F2} ms. " +
            "Window start {WindowStart:HH:mm:ss.fff}, Request in window {RequestNumber}",
            request.Id,
            elapsedMilliseconds,
            windowStart,
            requestNumber);        

        await Task.Delay(_settings.ProcessingDelayMilliseconds, cancellationToken);

        return true;
    }
}