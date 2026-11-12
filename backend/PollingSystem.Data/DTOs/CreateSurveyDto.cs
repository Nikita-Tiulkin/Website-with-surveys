namespace PollingSystem.Data.DTOs;

public class CreateSurveyDto
{
    public Guid AuthorId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? EndsAt { get; set; }
    public List<CreateQuestionDto> Questions { get; set; } = new();
}
