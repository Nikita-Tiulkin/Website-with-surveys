using PollingSystem.Data.Enums;

namespace PollingSystem.Data.Entities;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SurveyId { get; set; }
    public string Title { get; set; } = null!;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; } = true;
    public int Position { get; set; }
    public Survey Survey { get; set; } = null!;
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
