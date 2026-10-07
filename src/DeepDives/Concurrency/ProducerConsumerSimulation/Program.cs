using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProducerConsumerSimulation.Configuration;

namespace ProducerConsumerSimulation
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSimulation(context.Configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
