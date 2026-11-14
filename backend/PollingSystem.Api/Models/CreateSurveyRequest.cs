
using System.ComponentModel.DataAnnotations;

namespace PollingSystem.Api.Models;

public class CreateSurveyRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime? EndsAt { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateQuestionRequest> Questions { get; set; } = new();
}

public class CreateQuestionRequest
{
    [Required]
    [StringLength(500, MinimumLength = 2)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = "single";

    public bool IsRequired { get; set; } = true;

    public List<CreateOptionRequest> Options { get; set; } = new();
}

public class CreateOptionRequest
{
    [Required]
    [StringLength(300, MinimumLength = 1)]
    public string Text { get; set; } = string.Empty;
}
