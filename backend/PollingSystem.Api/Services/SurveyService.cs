
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PollingSystem.Api.Models;
using PollingSystem.Api.Services.Interfaces;
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;
using PollingSystem.Data.Enums;
using PollingSystem.Data.Repositories;

namespace PollingSystem.Api.Services;

public sealed class SurveyService : ISurveyService
{
    private readonly ISurveyRepository _surveys;

    public SurveyService(ISurveyRepository surveys)
    {
        _surveys = surveys;
    }

    public async Task<List<SurveyDto>> GetAllAsync(
        Guid? authorId,
        CancellationToken cancellationToken = default)
    {
        List<Survey> surveys;

        if (authorId.HasValue)
        {
            surveys = await _surveys.GetByAuthorIdAsync(
                authorId.Value,
                cancellationToken);
        }
        else
        {
            surveys = await _surveys.GetAllAsync(cancellationToken);
        }

        return surveys.Select(MapSurvey).ToList();
    }

    public async Task<SurveyDto?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        Survey? survey = await _surveys.GetByIdAsync(id, cancellationToken);

        if (survey is null)
        {
            return null;
        }

        SurveyDto result = MapSurvey(survey);

        result.HasVoted = await _surveys.HasUserVotedAsync(
            id,
            userId,
            cancellationToken);

        return result;
    }

    public async Task<SurveyDto> CreateAsync(
        Guid authorId,
        CreateSurveyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Questions is null || request.Questions.Count == 0)
        {
            throw new InvalidOperationException(
                "Опрос должен содержать хотя бы один вопрос.");
        }

        List<QuestionType> questionTypes = new();

        foreach (CreateQuestionRequest question in request.Questions)
        {
            if (string.Equals(
                question.Type,
                "single",
                StringComparison.OrdinalIgnoreCase))
            {
                questionTypes.Add(QuestionType.SingleChoice);
            }
            else if (string.Equals(
                question.Type,
                "multiple",
                StringComparison.OrdinalIgnoreCase))
            {
                questionTypes.Add(QuestionType.MultipleChoice);
            }
            else if (string.Equals(
                question.Type,
                "text",
                StringComparison.OrdinalIgnoreCase))
            {
                questionTypes.Add(QuestionType.Text);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Неизвестный тип вопроса: {question.Type}");
            }

            if (questionTypes[^1] != QuestionType.Text &&
                (question.Options is null || question.Options.Count < 2))
            {
                throw new InvalidOperationException(
                    "В вопросах с вариантами ответа должно быть не менее двух вариантов.");
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

        Survey createdSurvey = await _surveys.AddAsync(
            dto,
            cancellationToken);

        return MapSurvey(createdSurvey);
    }

    public async Task<SurveyResponse> VoteAsync(
        Guid surveyId,
        Guid userId,
        SubmitVoteDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Survey? survey = await _surveys.GetByIdAsync(
            surveyId,
            cancellationToken);

        if (survey is null)
        {
            throw new KeyNotFoundException("Опрос не найден.");
        }

        if (survey.Status != SurveyStatus.Active)
        {
            throw new InvalidOperationException("Опрос закрыт.");
        }

        if (survey.EndsAt.HasValue &&
            survey.EndsAt.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Срок голосования истёк.");
        }

        bool alreadyVoted = await _surveys.HasUserVotedAsync(
            surveyId,
            userId,
            cancellationToken);

        if (alreadyVoted)
        {
            throw new InvalidOperationException(
                "Вы уже голосовали в этом опросе.");
        }

        if (request.Answers is null || request.Answers.Count == 0)
        {
            throw new InvalidOperationException(
                "Необходимо отправить ответы.");
        }

        if (request.Answers
            .GroupBy(answer => answer.QuestionId)
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "Ответ на один вопрос указан несколько раз.");
        }

        HashSet<Guid> questionIds = survey.Questions
            .Select(question => question.Id)
            .ToHashSet();

        if (request.Answers.Any(answer =>
            !questionIds.Contains(answer.QuestionId)))
        {
            throw new InvalidOperationException(
                "Опрос содержит неизвестный вопрос.");
        }

        foreach (Question question in survey.Questions)
        {
            SubmitAnswerDto? submittedAnswer = request.Answers
                .SingleOrDefault(answer =>
                    answer.QuestionId == question.Id);

            if (submittedAnswer is null)
            {
                if (question.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Необходимо ответить на вопрос: {question.Title}");
                }

                continue;
            }

            List<Guid> selectedIds =
                submittedAnswer.SelectedOptionIds ?? new List<Guid>();

            if (selectedIds.Distinct().Count() != selectedIds.Count)
            {
                throw new InvalidOperationException(
                    "Один вариант ответа выбран несколько раз.");
            }

            if (question.Type == QuestionType.Text)
            {
                if (selectedIds.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Для текстового вопроса нельзя выбирать варианты ответа.");
                }

                if (question.IsRequired &&
                    string.IsNullOrWhiteSpace(submittedAnswer.TextValue))
                {
                    throw new InvalidOperationException(
                        $"Необходимо заполнить вопрос: {question.Title}");
                }

                continue;
            }

            if (!string.IsNullOrWhiteSpace(submittedAnswer.TextValue))
            {
                throw new InvalidOperationException(
                    "Для вопроса с вариантами нельзя отправлять текстовый ответ.");
            }

            if (question.IsRequired && selectedIds.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Необходимо выбрать ответ на вопрос: {question.Title}");
            }

            if (question.Type == QuestionType.SingleChoice &&
                selectedIds.Count > 1)
            {
                throw new InvalidOperationException(
                    "В этом вопросе можно выбрать только один вариант.");
            }

            HashSet<Guid> availableOptionIds = question.Options
                .Select(option => option.Id)
                .ToHashSet();

            if (selectedIds.Any(optionId =>
                !availableOptionIds.Contains(optionId)))
            {
                throw new InvalidOperationException(
                    "Выбранный вариант не принадлежит этому вопросу.");
            }
        }

        SurveyResponse response = new SurveyResponse
        {
            SurveyId = surveyId,
            UserId = userId,
            Answers = request.Answers.Select(submittedAnswer =>
            {
                Question question = survey.Questions.Single(
                    item => item.Id == submittedAnswer.QuestionId);

                return new Answer
                {
                    QuestionId = question.Id,
                    TextValue = question.Type == QuestionType.Text
                        ? string.IsNullOrWhiteSpace(submittedAnswer.TextValue)
                            ? null
                            : submittedAnswer.TextValue.Trim()
                        : null,
                    SelectedOptions = (
                        submittedAnswer.SelectedOptionIds ?? new List<Guid>())
                        .Select(optionId => new AnswerSelectedOption
                        {
                            OptionId = optionId
                        }).ToList()
                };
            }).ToList()
        };

        try
        {
            await _surveys.AddResponseAsync(
                response,
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException postgresException &&
                  postgresException.SqlState == PostgresErrorCodes.UniqueViolation &&
                  postgresException.ConstraintName == "ux_survey_responses_survey_user")
        {
            throw new InvalidOperationException(
                "Вы уже голосовали в этом опросе.");
        }

        return response;
    }

    public Task<SurveyStatsDto?> GetStatsAsync(
        Guid surveyId,
        CancellationToken cancellationToken = default)
    {
        return _surveys.GetStatsAsync(
            surveyId,
            cancellationToken);
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
