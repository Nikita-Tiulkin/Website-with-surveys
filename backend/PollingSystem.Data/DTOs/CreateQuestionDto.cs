using PollingSystem.Data.Enums;

namespace PollingSystem.Data.DTOs;

public class CreateQuestionDto
{
    public string Title { get; set; } = null!;
    public QuestionType Type { get; set; }
    public bool IsRequired { get; set; } = true;
    public List<CreateQuestionOptionDto> Options { get; set; } = new();
}
