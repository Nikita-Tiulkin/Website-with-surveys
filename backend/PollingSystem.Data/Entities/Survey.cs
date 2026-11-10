using PollingSystem.Data.Enums;

namespace PollingSystem.Data.Entities;

public class Survey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public SurveyStatus Status { get; set; } = SurveyStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndsAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public User Author { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<SurveyResponse> Responses { get; set; } = new List<SurveyResponse>();
}
