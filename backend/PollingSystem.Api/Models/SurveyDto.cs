
namespace PollingSystem.Api.Models;

public class SurveyDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid CreatorId { get; set; }
    public string? Description { get; set; }
    public List<SurveyQuestionDto> Questions { get; set; } = new();
}

public class SurveyQuestionDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<SurveyOptionDto> Options { get; set; } = new();
}

public class SurveyOptionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
