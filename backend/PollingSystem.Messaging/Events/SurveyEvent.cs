namespace PollingSystem.Messaging.Events;

public sealed class SurveyEvent
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public Guid SurveyId { get; set; }
    public string SurveyTitle { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}
