using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ProducerConsumerSimulation.Domain;
using ProducerConsumerSimulation.Services;
using ProducerConsumerSimulation.Workers;
using ChannelConsumer = ProducerConsumerSimulation.ChannelUnbounded.Consumer;
using ChannelProducer = ProducerConsumerSimulation.ChannelUnbounded.Producer;
using NaiveConsumer = ProducerConsumerSimulation.Naive.Consumer;
using NaiveProducer = ProducerConsumerSimulation.Naive.Producer;

namespace ProducerConsumerSimulation.Configuration
{
    /// <summary>
    /// Provides dependency injection registrations for the butcher shop simulation.
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
            services.Configure<ProducerConsumerSettings>(configuration.GetSection(ProducerConsumerSettings.SectionName));

            services.AddSingleton<Queue<WorkItem>>();
            services.AddSingleton<NaiveProducer>();
            services.AddSingleton<NaiveConsumer>();

            services.AddSingleton(Channel.CreateUnbounded<WorkItem>());            
            services.AddSingleton<ChannelProducer>();
            services.AddSingleton<ChannelConsumer>();

            services.AddSingleton<ChannelBounded.Producer>();
            services.AddSingleton<ChannelBounded.Consumer>();

            services.AddSingleton<Simulation>();
            services.AddHostedService<SimulationWorker>();

            return services;
        }
    }
}
