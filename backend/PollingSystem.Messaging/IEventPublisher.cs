using PollingSystem.Messaging.Events;

namespace PollingSystem.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(
        string queueName,
        SurveyEvent surveyEvent,
        CancellationToken cancellationToken = default);
}
