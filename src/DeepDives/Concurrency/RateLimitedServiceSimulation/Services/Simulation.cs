using RateLimitedServiceSimulation.Configuration;
using RateLimitedServiceSimulation.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PaymentNaiveService = RateLimitedServiceSimulation.Naive.PaymentService;
using PaymentConcurrenceLimitedService = RateLimitedServiceSimulation.ConcurrenceLimited.PaymentService;
using PaymentFixedWindowService = RateLimitedServiceSimulation.FixedWindow.PaymentService;
using PaymentSlidingWindowService = RateLimitedServiceSimulation.SlidingWindow.PaymentService;
using PaymentTokenBucketService = RateLimitedServiceSimulation.TokenBucket.PaymentService;

namespace RateLimitedServiceSimulation.Services;

public sealed class Simulation
{
    private readonly PaymentNaiveService _paymentNaiveService;
    private readonly PaymentConcurrenceLimitedService _paymentLimitedService;
    private readonly PaymentFixedWindowService _paymentFixedWindowService;
    private readonly PaymentSlidingWindowService _paymentSlidingWindowService;
    private readonly PaymentTokenBucketService _paymentTokenBucketService;
    private readonly RateLimitedServiceSettings _settings;
    private readonly ILogger<Simulation> _logger;

    public Simulation(
        IOptions<RateLimitedServiceSettings> options, 
        ILogger<Simulation> logger, 
        PaymentNaiveService paymentNaiveService, 
        PaymentConcurrenceLimitedService paymentLimitedService,
        PaymentFixedWindowService paymentFixedWindowService,
        PaymentSlidingWindowService paymentSlidingWindowService,
        PaymentTokenBucketService paymentTokenBucketService)
    {
        _settings = options.Value;
        _logger = logger;
        _paymentNaiveService = paymentNaiveService;
        _paymentLimitedService = paymentLimitedService;
        _paymentFixedWindowService = paymentFixedWindowService;
        _paymentSlidingWindowService = paymentSlidingWindowService;
        _paymentTokenBucketService = paymentTokenBucketService;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunTokenBucketAsync(cancellationToken);
    }

    public async Task RunNaiveAsync(CancellationToken cancellationToken)
    {
        var requests = Enumerable
            .Range(1, _settings.RequestCount)
            .Select(id => new PaymentRequest(
                id,
                Amount: 100))
            .ToArray();

        var tasks = requests
            .Select(request => _paymentNaiveService.ProcessAsync(request, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var accepted = results.Count(result => result);
        var rejected = results.Length - accepted;

        _logger.LogInformation("Simulation completed. Accepted: {Accepted}, Rejected: {Rejected}", accepted, rejected);
    }

    public async Task RunLimitedAsync(CancellationToken cancellationToken)
    {
        var requests = Enumerable
            .Range(1, _settings.RequestCount)
            .Select(id => new PaymentRequest(
                id,
                Amount: 100))
            .ToArray();

        var tasks = requests
            .Select(request => _paymentLimitedService.ProcessAsync(request, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var accepted = results.Count(result => result);
        var rejected = results.Length - accepted;

        _logger.LogInformation("Simulation completed. Accepted: {Accepted}, Rejected: {Rejected}", accepted, rejected);
    }

    public async Task RunFixedWindowAsync(CancellationToken cancellationToken)
    {
        var requests = Enumerable
            .Range(1, _settings.RequestCount)
            .Select(id => new PaymentRequest(
                id,
                Amount: 100))
            .ToArray();

        var tasks = requests
            .Select(request => _paymentFixedWindowService.ProcessAsync(request, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var accepted = results.Count(result => result);
        var rejected = results.Length - accepted;

        _logger.LogInformation("Simulation completed. Accepted: {Accepted}, Rejected: {Rejected}", accepted, rejected);
    }

    public async Task RunSlidingWindowAsync(CancellationToken cancellationToken)
    {
        var requests = Enumerable
            .Range(1, _settings.RequestCount)
            .Select(id => new PaymentRequest(
                id,
                Amount: 100))
            .ToArray();

        var tasks = requests
            .Select(request => _paymentSlidingWindowService.ProcessAsync(request, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var accepted = results.Count(result => result);
        var rejected = results.Length - accepted;

        _logger.LogInformation("Simulation completed. Accepted: {Accepted}, Rejected: {Rejected}", accepted, rejected);
    }

    public async Task RunTokenBucketAsync(CancellationToken cancellationToken)
    {
        var requests = Enumerable
            .Range(1, _settings.RequestCount)
            .Select(id => new PaymentRequest(
                id,
                Amount: 100))
            .ToArray();

        var tasks = requests
            .Select(request => _paymentTokenBucketService.ProcessAsync(request, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        var accepted = results.Count(result => result);
        var rejected = results.Length - accepted;

        _logger.LogInformation("Simulation completed. Accepted: {Accepted}, Rejected: {Rejected}", accepted, rejected);
    }


}