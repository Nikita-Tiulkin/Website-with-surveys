namespace PollingSystem.Data.Entities;

public class AnswerSelectedOption
{
    public Guid AnswerId { get; set; }
    public Guid OptionId { get; set; }
    public Answer Answer { get; set; } = null!;
    public QuestionOption Option { get; set; } = null!;
}
