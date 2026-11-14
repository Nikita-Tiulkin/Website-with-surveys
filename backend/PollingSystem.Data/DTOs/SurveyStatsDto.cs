
namespace PollingSystem.Data.DTOs;

public class SurveyStatsDto
{
    public int TotalVotes { get; set; }

    public List<QuestionStatsDto> Questions { get; set; } = new();
}

public class QuestionStatsDto
{
    public Guid Id { get; set; }

    public List<OptionStatsDto> Options { get; set; } = new();
}

public class OptionStatsDto
{
    public Guid Id { get; set; }

    public int Count { get; set; }

    public double Percent { get; set; }
}
