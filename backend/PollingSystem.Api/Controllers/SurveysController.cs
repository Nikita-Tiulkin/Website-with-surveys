
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PollingSystem.Api.Models;
using PollingSystem.Api.Services.Interfaces;
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;

namespace PollingSystem.Api.Controllers;

[ApiController]
[Route("api/surveys")]
public class SurveysController : ControllerBase
{
    private readonly ISurveyService _surveys;

    public SurveysController(ISurveyService surveys)
    {
        _surveys = surveys;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool mine = false,
        CancellationToken cancellationToken = default)
    {
        Guid? authorId = null;

        if (mine)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out Guid parsedAuthorId))
            {
                return Unauthorized(new
                {
                    error = "Необходимо авторизоваться."
                });
            }

            authorId = parsedAuthorId;
        }

        List<SurveyDto> surveys = await _surveys.GetAllAsync(
            authorId,
            cancellationToken);

        return Ok(surveys);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userId, out Guid currentUserId))
        {
            return Unauthorized(new
            {
                error = "Необходимо авторизоваться."
            });
        }

        SurveyDto? survey = await _surveys.GetByIdAsync(
            id,
            currentUserId,
            cancellationToken);

        if (survey is null)
        {
            return NotFound(new
            {
                error = "Опрос не найден."
            });
        }

        return Ok(survey);
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
            return Unauthorized(new
            {
                error = "Не удалось определить пользователя."
            });
        }

        try
        {
            SurveyDto createdSurvey = await _surveys.CreateAsync(
                authorId,
                request,
                cancellationToken);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdSurvey.Id },
                createdSurvey);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
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
            return Unauthorized(new
            {
                error = "Не удалось определить пользователя."
            });
        }

        try
        {
            SurveyResponse response = await _surveys.VoteAsync(
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
            return NotFound(new
            {
                error = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new
            {
                error = exception.Message
            });
        }
        catch (DbUpdateException)
        {
            return Conflict(new
            {
                error = "Вы уже голосовали в этом опросе."
            });
        }
    }

    [HttpGet("{id:guid}/stats")]
    public async Task<IActionResult> GetStats(
        Guid id,
        CancellationToken cancellationToken)
    {
        SurveyStatsDto? stats = await _surveys.GetStatsAsync(
            id,
            cancellationToken);

        if (stats is null)
        {
            return NotFound(new
            {
                error = "Опрос не найден."
            });
        }

        return Ok(stats);
    }
}
