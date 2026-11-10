namespace PollingSystem.Data.Entities;

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = null!;
    public int Position { get; set; }
    public Question Question { get; set; } = null!;
    public ICollection<AnswerSelectedOption> SelectedInAnswers { get; set; } = new List<AnswerSelectedOption>();
}
