
namespace PollingSystem.Data.DTOs;

public class SubmitVoteDto
{
    public List<SubmitAnswerDto> Answers { get; set; } = new();
}

public class SubmitAnswerDto
{
    public Guid QuestionId { get; set; }
    public List<Guid> SelectedOptionIds { get; set; } = new();
    public string? TextValue { get; set; }
}
