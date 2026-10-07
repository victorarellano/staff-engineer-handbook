using Idempotency.Api.Messaging;
using Idempotency.Api.Repositories;

namespace Idempotency.Api.BackgroundServices;

public sealed class OutboxWorker(IServiceScopeFactory scopeFactory, IMessagePublisher publisher, ILogger<OutboxWorker> logger) : BackgroundService
{

    // Versión actual:
    // Reserva temporal + publicación + finalización.    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();

                await using var claimUnitOfWork = await unitOfWorkFactory.CreateAsync();
                var messages = await claimUnitOfWork.OutboxMessages.ClaimPendingAsync(DateTimeOffset.UtcNow.AddSeconds(30), batchSize: 10, stoppingToken);
                await claimUnitOfWork.CommitAsync();                

                logger.LogInformation("Worker found {Count} pending messages", messages.Count);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                foreach (var message in messages)
                {
                    try
                    {
                        await publisher.PublishAsync(message, stoppingToken);

                        await using var completeUnitOfWork  = await unitOfWorkFactory.CreateAsync();              

                        await completeUnitOfWork .OutboxMessages.MarkAsPublishedAsync(message.MessageId, DateTimeOffset.UtcNow, stoppingToken);
                        await completeUnitOfWork .CommitAsync();
                        logger.LogInformation("Outbox message {MessageId} published", message.MessageId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    // Versión 1:
    // Lee pendientes y publica.
    // Vulnerable a múltiples workers procesando el mismo mensaje.
    private async Task ExecuteWithoutClaimAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
                await using var unitOfWork = await unitOfWorkFactory.CreateAsync();
                
                var pendingMessages = await unitOfWork.OutboxMessages.GetPendingAsync(stoppingToken);

                logger.LogInformation("Worker found {Count} pending messages", pendingMessages.Count);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                foreach (var message in pendingMessages)
                {
                    try
                    {
                        await publisher.PublishAsync(message, stoppingToken);

                        await unitOfWork.OutboxMessages.MarkAsPublishedAsync(message.MessageId, DateTimeOffset.UtcNow, stoppingToken);
                        await unitOfWork.CommitAsync();
                        logger.LogInformation("Outbox message {MessageId} published", message.MessageId);

                        /*
                        await publisher.PublishAsync(message, stoppingToken);
                        logger.LogWarning("SIMULATED CRASH after publishing {MessageId}", message.MessageId);
                        Environment.FailFast($"Simulated crash after publishing {message.MessageId}");
                        */
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    // Versión 2:
    // Misma estrategia, utilizada para reproducir la ventana
    // publish → crash → DB todavía Pending.
    private async Task ExecuteCrashWindowDemoAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();

                var unitOfWorkFactory = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>();
                await using var unitOfWork = await unitOfWorkFactory.CreateAsync();
                
                var pendingMessages = await unitOfWork.OutboxMessages.GetPendingAsync(stoppingToken);

                logger.LogInformation("Worker found {Count} pending messages", pendingMessages.Count);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

                foreach (var message in pendingMessages)
                {
                    try
                    {
                        await publisher.PublishAsync(message, stoppingToken);
                        logger.LogWarning("SIMULATED CRASH after publishing {MessageId}", message.MessageId);
                        Environment.FailFast($"Simulated crash after publishing {message.MessageId}");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.MessageId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
