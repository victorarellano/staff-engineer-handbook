using RateLimitedServiceSimulation.Services;
using RateLimitedServiceSimulation.Workers;

using PaymentNaiveService = RateLimitedServiceSimulation.Naive.PaymentService;
using PaymentConcurrenceLimitedService = RateLimitedServiceSimulation.ConcurrenceLimited.PaymentService;
using PaymentFixedWindowService = RateLimitedServiceSimulation.FixedWindow.PaymentService;
using PaymentSlidingWindowService = RateLimitedServiceSimulation.SlidingWindow.PaymentService;
using PaymentTokenBucketService = RateLimitedServiceSimulation.TokenBucket.PaymentService;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RateLimitedServiceSimulation.Api;

namespace RateLimitedServiceSimulation.Configuration;

/// <summary>
/// Provides dependency injection registrations for the simulation.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the configuration and hosted services required by the simulation.
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">The application's configuration.</param>
    /// <returns>The same service collection, allowing chained registrations.</returns>
    public static IServiceCollection AddSimulation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RateLimitedServiceSettings>(configuration.GetSection(RateLimitedServiceSettings.SectionName));

        services.AddSingleton<Simulation>();
        services.AddSingleton<PaymentApi>();
        services.AddSingleton<PaymentNaiveService>();
        services.AddSingleton<PaymentConcurrenceLimitedService>();
        services.AddSingleton<PaymentFixedWindowService>();
        services.AddSingleton<PaymentSlidingWindowService>();
        services.AddSingleton<PaymentTokenBucketService>();
        
        services.AddHostedService<SimulationWorker>();

        return services;
    }
}
