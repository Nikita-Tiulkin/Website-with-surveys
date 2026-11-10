namespace PollingSystem.Data.Entities;

public class Answer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ResponseId { get; set; }
    public Guid QuestionId { get; set; }
    public string? TextValue { get; set; }
    public SurveyResponse Response { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public ICollection<AnswerSelectedOption> SelectedOptions { get; set; } = new List<AnswerSelectedOption>();
}
