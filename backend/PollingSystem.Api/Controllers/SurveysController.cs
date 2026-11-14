
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollingSystem.Api.Models;
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;
using PollingSystem.Data.Enums;
using PollingSystem.Data.Repositories;

namespace PollingSystem.Api.Controllers;

[ApiController]
[Route("api/surveys")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyRepository _surveys;

    public SurveysController(ISurveyRepository surveys)
    {
        _surveys = surveys;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool mine = false,
        CancellationToken cancellationToken = default)
    {
        List<Survey> surveys;

        if (mine)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out Guid authorId))
            {
                return Unauthorized(new { error = "Необходимо авторизоваться." });
            }

            surveys = await _surveys.GetByAuthorIdAsync(authorId, cancellationToken);
        }
        else
        {
            surveys = await _surveys.GetAllAsync(cancellationToken);
        }

        return Ok(surveys.Select(MapSurvey));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        Survey? survey = await _surveys.GetByIdAsync(id, cancellationToken);

        if (survey is null)
        {
            return NotFound(new { error = "Опрос не найден." });
        }

        return Ok(MapSurvey(survey));
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateSurveyRequest request,
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out Guid authorId))
        {
            return Unauthorized(new { error = "Не удалось определить пользователя." });
        }

        if (request.Questions is null || request.Questions.Count == 0)
        {
            return BadRequest(new { error = "Опрос должен содержать хотя бы один вопрос." });
        }

        List<QuestionType> questionTypes = new();

        foreach (CreateQuestionRequest question in request.Questions)
        {
            if (question.Type.Equals("single", StringComparison.OrdinalIgnoreCase))
            {
                questionTypes.Add(QuestionType.SingleChoice);
            }
            else if (question.Type.Equals("multiple", StringComparison.OrdinalIgnoreCase))
            {
                questionTypes.Add(QuestionType.MultipleChoice);
            }
            else if (question.Type.Equals("text", StringComparison.OrdinalIgnoreCase))
            {
                questionTypes.Add(QuestionType.Text);
            }
            else
            {
                return BadRequest(new { error = $"Неизвестный тип вопроса: {question.Type}" });
            }

            if (questionTypes[^1] != QuestionType.Text &&
                (question.Options is null || question.Options.Count < 2))
            {
                return BadRequest(new
                {
                    error = "В вопросах с вариантами ответа должно быть не менее двух вариантов."
                });
            }
        }

        CreateSurveyDto dto = new CreateSurveyDto
        {
            AuthorId = authorId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            EndsAt = request.EndsAt,
            Questions = request.Questions.Select((question, index) =>
                new CreateQuestionDto
                {
                    Title = question.Title.Trim(),
                    Type = questionTypes[index],
                    IsRequired = question.IsRequired,
                    Options = question.Options?.Select(option =>
                        new CreateQuestionOptionDto
                        {
                            Text = option.Text.Trim()
                        }).ToList() ?? new List<CreateQuestionOptionDto>()
                }).ToList()
        };

        Survey createdSurvey = await _surveys.AddAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdSurvey.Id },
            MapSurvey(createdSurvey));
    }

    [Authorize]
    [HttpPost("{id:guid}/votes")]
    public async Task<IActionResult> Vote(
        Guid id,
        [FromBody] SubmitVoteDto request,
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out Guid voterId))
        {
            return Unauthorized(new { error = "Не удалось определить пользователя." });
        }

        try
        {
            SurveyResponse response = await _surveys.AddResponseAsync(
                id,
                voterId,
                request,
                cancellationToken);

            return Ok(new
            {
                message = "Ваш голос успешно сохранён.",
                responseId = response.Id
            });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "Вы уже голосовали в этом опросе." });
        }
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<IActionResult> GetStats(
        Guid id,
        CancellationToken cancellationToken)
    {
        SurveyStatsDto? stats = await _surveys.GetStatsAsync(id, cancellationToken);

        if (stats is null)
        {
            return NotFound(new { error = "Опрос не найден." });
        }

        return Ok(stats);
    }

    private static SurveyDto MapSurvey(Survey survey)
    {
        return new SurveyDto
        {
            Id = survey.Id,
            Title = survey.Title,
            CreatorId = survey.AuthorId,
            Description = survey.Description,
            Questions = survey.Questions
                .OrderBy(question => question.Position)
                .Select(question => new SurveyQuestionDto
                {
                    Id = question.Id,
                    Title = question.Title,
                    Type = MapQuestionType(question.Type),
                    Options = question.Options
                        .OrderBy(option => option.Position)
                        .Select(option => new SurveyOptionDto
                        {
                            Id = option.Id,
                            Text = option.Text
                        }).ToList()
                }).ToList()
        };
    }

    private static string MapQuestionType(QuestionType type)
    {
        return type switch
        {
            QuestionType.SingleChoice => "single",
            QuestionType.MultipleChoice => "multiple",
            QuestionType.Text => "text",
            _ => type.ToString()
        };
    }
}
