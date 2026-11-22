using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PollingSystem.Data;
using PollingSystem.Data.Entities;
using PollingSystem.Messaging;
using PollingSystem.Messaging.Events;
using PollingSystem.Messaging.Queues;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PollingSystem.Worker.Services;

public sealed class SurveyEventConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _publisher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SurveyEventConsumer> _logger;

    public SurveyEventConsumer(
        IServiceScopeFactory scopeFactory,
        IEventPublisher publisher,
        IConfiguration configuration,
        ILogger<SurveyEventConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? connection = null;
            IChannel? channel = null;

            try
            {
                ConnectionFactory factory = CreateConnectionFactory();
                connection = await factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(
                    cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    QueueNames.SurveyEvents,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: 1,
                    global: false,
                    cancellationToken: stoppingToken);

                AsyncEventingBasicConsumer consumer = new(channel);
                consumer.ReceivedAsync += async (_, args) =>
                {
                    SurveyEvent? surveyEvent = null;

                    try
                    {
                        byte[] body = args.Body.ToArray();
                        surveyEvent = JsonSerializer.Deserialize<SurveyEvent>(body);

                        if (surveyEvent is null || surveyEvent.EventId == Guid.Empty)
                        {
                            throw new InvalidOperationException("Получено некорректное событие.");
                        }

                        await using AsyncServiceScope scope =
                            _scopeFactory.CreateAsyncScope();
                        AppDbContext db = scope.ServiceProvider
                            .GetRequiredService<AppDbContext>();

                        bool alreadyProcessed = await db.ProcessedEvents
                            .AnyAsync(item => item.EventId == surveyEvent.EventId,
                                stoppingToken);

                        if (!alreadyProcessed)
                        {
                            await _publisher.PublishAsync(
                                QueueNames.SurveyNotifications,
                                surveyEvent,
                                stoppingToken);

                            db.ProcessedEvents.Add(new ProcessedEvent
                            {
                                EventId = surveyEvent.EventId,
                                ProcessedAt = DateTime.UtcNow
                            });
                            await db.SaveChangesAsync(stoppingToken);
                        }

                        await channel.BasicAckAsync(args.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception,
                            "Не удалось обработать событие {EventId}.",
                            surveyEvent?.EventId);

                        await channel.BasicNackAsync(
                            args.DeliveryTag,
                            multiple: false,
                            requeue: true,
                            cancellationToken: CancellationToken.None);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: QueueNames.SurveyEvents,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("Worker читает очередь {Queue}.",
                    QueueNames.SurveyEvents);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Ошибка подключения к RabbitMQ. Повтор через 5 секунд.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            finally
            {
                if (channel is not null)
                {
                    await channel.DisposeAsync();
                }

                if (connection is not null)
                {
                    await connection.DisposeAsync();
                }
            }
        }
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"] ?? "localhost",
            Port = int.TryParse(_configuration["RabbitMq:Port"], out int rabbitPort)
                ? rabbitPort
                : 5672,
            UserName = _configuration["RabbitMq:UserName"] ?? "guest",
            Password = _configuration["RabbitMq:Password"] ?? "guest",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            ClientProvidedName = "PollingSystem-worker-consumer"
        };
    }
}
