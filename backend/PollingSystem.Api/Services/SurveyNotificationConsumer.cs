using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PollingSystem.Api.Hubs;
using PollingSystem.Messaging.Events;
using PollingSystem.Messaging.Queues;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PollingSystem.Api.Services;

public sealed class SurveyNotificationConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IHubContext<SurveyHub> _hub;
    private readonly ILogger<SurveyNotificationConsumer> _logger;

    public SurveyNotificationConsumer(
        IConfiguration configuration,
        IHubContext<SurveyHub> hub,
        ILogger<SurveyNotificationConsumer> logger)
    {
        _configuration = configuration;
        _hub = hub;
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
                ConnectionFactory factory = new()
                {
                    HostName = _configuration["RabbitMq:Host"] ?? "localhost",
                    Port = _configuration.GetValue("RabbitMq:Port", 5672),
                    UserName = _configuration["RabbitMq:UserName"] ?? "guest",
                    Password = _configuration["RabbitMq:Password"] ?? "guest",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    ClientProvidedName = "PollingSystem-api-notifications"
                };

                connection = await factory.CreateConnectionAsync(stoppingToken);
                channel = await connection.CreateChannelAsync(
                    cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    QueueNames.SurveyNotifications,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                AsyncEventingBasicConsumer consumer = new(channel);
                consumer.ReceivedAsync += async (_, args) =>
                {
                    try
                    {
                        byte[] body = args.Body.ToArray();
                        SurveyEvent? surveyEvent =
                            JsonSerializer.Deserialize<SurveyEvent>(body);

                        if (surveyEvent is null)
                        {
                            throw new InvalidOperationException("Получено пустое уведомление.");
                        }

                        await _hub.Clients.All.SendAsync(
                            "SurveyChanged",
                            new
                            {
                                eventId = surveyEvent.EventId,
                                eventType = surveyEvent.EventType,
                                surveyId = surveyEvent.SurveyId,
                                surveyTitle = surveyEvent.SurveyTitle,
                                userId = surveyEvent.UserId
                            },
                            stoppingToken);

                        await channel.BasicAckAsync(
                            args.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(exception, "Не удалось отправить SignalR-уведомление.");
                        await channel.BasicNackAsync(
                            args.DeliveryTag,
                            multiple: false,
                            requeue: true,
                            cancellationToken: CancellationToken.None);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: QueueNames.SurveyNotifications,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.LogInformation("API читает очередь уведомлений {Queue}.",
                    QueueNames.SurveyNotifications);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception,
                    "Не удалось подключиться к очереди уведомлений. Повтор через 5 секунд.");
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
}
