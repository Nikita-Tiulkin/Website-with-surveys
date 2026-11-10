namespace PollingSystem.Data.Entities;

public class SurveyResponse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SurveyId { get; set; }
    public Guid UserId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Survey Survey { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
