using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PollingSystem.Data;
using PollingSystem.Data.Enums;
using PollingSystem.Messaging;
using PollingSystem.Messaging.Events;
using PollingSystem.Messaging.Queues;

namespace PollingSystem.Worker.Services;

public sealed class ExpiredSurveyCloser : BackgroundService
{
    private const string LeaseName = "close-expired-surveys";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<ExpiredSurveyCloser> _logger;
    private readonly Guid _ownerId = Guid.NewGuid();

    public ExpiredSurveyCloser(
        IServiceScopeFactory scopeFactory,
        IEventPublisher publisher,
        ILogger<ExpiredSurveyCloser> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CloseExpiredSurveysAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Ошибка при закрытии просроченных опросов.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CloseExpiredSurveysAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        DateTime now = DateTime.UtcNow;
        DateTime leaseExpiresAt = now.Add(LeaseDuration);

        int leaseAcquired = await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO worker_leases (name, owner_id, expires_at)
            VALUES ({LeaseName}, {_ownerId}, {leaseExpiresAt})
            ON CONFLICT (name) DO UPDATE
                SET owner_id = EXCLUDED.owner_id,
                    expires_at = EXCLUDED.expires_at
            WHERE worker_leases.expires_at <= {now}
               OR worker_leases.owner_id = {_ownerId};",
            cancellationToken);

        if (leaseAcquired == 0)
        {
            return;
        }

        try
        {
            List<PollingSystem.Data.Entities.Survey> expiredSurveys =
                await db.Surveys
                    .Where(survey => survey.Status == SurveyStatus.Active &&
                                     survey.EndsAt.HasValue &&
                                     survey.EndsAt.Value <= now)
                    .ToListAsync(cancellationToken);

            foreach (var survey in expiredSurveys)
            {
                survey.Status = SurveyStatus.Closed;
                survey.ClosedAt = now;
            }

            await db.SaveChangesAsync(cancellationToken);

            foreach (var survey in expiredSurveys)
            {
                await _publisher.PublishAsync(
                    QueueNames.SurveyNotifications,
                    new SurveyEvent
                    {
                        EventType = SurveyEventTypes.Closed,
                        SurveyId = survey.Id,
                        SurveyTitle = survey.Title,
                        OccurredAtUtc = now
                    },
                    cancellationToken);
            }

            if (expiredSurveys.Count > 0)
            {
                _logger.LogInformation("Закрыто просроченных опросов: {Count}.",
                    expiredSurveys.Count);
            }
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE worker_leases
                SET expires_at = {DateTime.UtcNow}
                WHERE name = {LeaseName} AND owner_id = {_ownerId};",
                cancellationToken);
        }
    }
}
