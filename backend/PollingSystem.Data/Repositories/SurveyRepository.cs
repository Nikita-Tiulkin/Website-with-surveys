
using Microsoft.EntityFrameworkCore;
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;
using PollingSystem.Data.Enums;

namespace PollingSystem.Data.Repositories;

public sealed class SurveyRepository(AppDbContext context) : ISurveyRepository
{
    public async Task<Survey> AddAsync(
        CreateSurveyDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        Survey survey = new Survey
        {
            AuthorId = dto.AuthorId,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            EndsAt = dto.EndsAt,
            Status = SurveyStatus.Active,
            Questions = dto.Questions.Select((question, questionIndex) =>
                new Question
                {
                    Title = question.Title.Trim(),
                    Type = question.Type,
                    IsRequired = question.IsRequired,
                    Position = questionIndex,
                    Options = question.Options.Select((option, optionIndex) =>
                        new QuestionOption
                        {
                            Text = option.Text.Trim(),
                            Position = optionIndex
                        }).ToList()
                }).ToList()
        };

        await context.Surveys.AddAsync(survey, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return survey;
    }

    public async Task<Survey?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await context.Surveys
            .AsNoTracking()
            .Include(survey => survey.Questions
                .OrderBy(question => question.Position))
            .ThenInclude(question => question.Options
                .OrderBy(option => option.Position))
            .SingleOrDefaultAsync(survey => survey.Id == id, cancellationToken);
    }

    public async Task<List<Survey>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.Surveys
            .AsNoTracking()
            .Where(survey => survey.Status == SurveyStatus.Active)
            .Include(survey => survey.Questions
                .OrderBy(question => question.Position))
            .ThenInclude(question => question.Options
                .OrderBy(option => option.Position))
            .OrderByDescending(survey => survey.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Survey>> GetByAuthorIdAsync(
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        return await context.Surveys
            .AsNoTracking()
            .Where(survey => survey.AuthorId == authorId)
            .Include(survey => survey.Questions
                .OrderBy(question => question.Position))
            .ThenInclude(question => question.Options
                .OrderBy(option => option.Position))
            .OrderByDescending(survey => survey.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<SurveyResponse> AddResponseAsync(
        Guid surveyId,
        Guid userId,
        SubmitVoteDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        Survey? survey = await context.Surveys
            .Include(item => item.Questions)
                .ThenInclude(question => question.Options)
            .SingleOrDefaultAsync(item => item.Id == surveyId, cancellationToken);

        if (survey is null)
        {
            throw new KeyNotFoundException("Опрос не найден.");
        }

        if (survey.Status != SurveyStatus.Active)
        {
            throw new InvalidOperationException("Опрос закрыт.");
        }

        if (survey.EndsAt.HasValue && survey.EndsAt.Value <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Срок голосования истёк.");
        }

        bool alreadyVoted = await context.SurveyResponses
            .AnyAsync(
                response => response.SurveyId == surveyId && response.UserId == userId,
                cancellationToken);

        if (alreadyVoted)
        {
            throw new InvalidOperationException("Вы уже голосовали в этом опросе.");
        }

        if (dto.Answers is null || dto.Answers.Count == 0)
        {
            throw new InvalidOperationException("Необходимо отправить ответы.");
        }

        if (dto.Answers.GroupBy(answer => answer.QuestionId).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("Ответ на один вопрос указан несколько раз.");
        }

        HashSet<Guid> questionIds = survey.Questions.Select(question => question.Id).ToHashSet();

        if (dto.Answers.Any(answer => !questionIds.Contains(answer.QuestionId)))
        {
            throw new InvalidOperationException("Опрос содержит неизвестный вопрос.");
        }

        foreach (Question question in survey.Questions)
        {
            SubmitAnswerDto? submittedAnswer = dto.Answers
                .SingleOrDefault(answer => answer.QuestionId == question.Id);

            if (submittedAnswer is null)
            {
                if (question.IsRequired)
                {
                    throw new InvalidOperationException(
                        $"Необходимо ответить на вопрос: {question.Title}");
                }

                continue;
            }

            List<Guid> selectedIds = submittedAnswer.SelectedOptionIds ?? new List<Guid>();

            if (selectedIds.Distinct().Count() != selectedIds.Count)
            {
                throw new InvalidOperationException("Один вариант ответа выбран несколько раз.");
            }

            if (question.Type == QuestionType.Text)
            {
                if (selectedIds.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Для текстового вопроса нельзя выбирать варианты ответа.");
                }

                if (question.IsRequired && string.IsNullOrWhiteSpace(submittedAnswer.TextValue))
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

            if (question.Type == QuestionType.SingleChoice && selectedIds.Count > 1)
            {
                throw new InvalidOperationException(
                    "В этом вопросе можно выбрать только один вариант.");
            }

            HashSet<Guid> availableOptionIds = question.Options
                .Select(option => option.Id)
                .ToHashSet();

            if (selectedIds.Any(optionId => !availableOptionIds.Contains(optionId)))
            {
                throw new InvalidOperationException(
                    "Выбранный вариант не принадлежит этому вопросу.");
            }
        }

        SurveyResponse response = new SurveyResponse
        {
            SurveyId = surveyId,
            UserId = userId,
            Answers = dto.Answers.Select(submittedAnswer =>
            {
                Question question = survey.Questions.Single(
                    item => item.Id == submittedAnswer.QuestionId);

                return new Answer
                {
                    QuestionId = question.Id,
                    TextValue = question.Type == QuestionType.Text
                        ? submittedAnswer.TextValue?.Trim()
                        : null,
                    SelectedOptions = (submittedAnswer.SelectedOptionIds ?? new List<Guid>())
                        .Select(optionId => new AnswerSelectedOption
                        {
                            OptionId = optionId
                        }).ToList()
                };
            }).ToList()
        };

        await context.SurveyResponses.AddAsync(response, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<SurveyStatsDto?> GetStatsAsync(
        Guid surveyId,
        CancellationToken cancellationToken = default)
    {
        Survey? survey = await context.Surveys
            .AsNoTracking()
            .Include(item => item.Questions)
                .ThenInclude(question => question.Options)
            .SingleOrDefaultAsync(item => item.Id == surveyId, cancellationToken);

        if (survey is null)
        {
            return null;
        }

        int totalVotes = await context.SurveyResponses
            .CountAsync(response => response.SurveyId == surveyId, cancellationToken);

        List<QuestionStatsDto> questionStats = new();

        foreach (Question question in survey.Questions.OrderBy(item => item.Position))
        {
            List<OptionStatsDto> optionStats = new();

            foreach (QuestionOption option in question.Options.OrderBy(item => item.Position))
            {
                int count = await context.AnswerSelectedOptions
                    .CountAsync(
                        selected => selected.OptionId == option.Id,
                        cancellationToken);

                double percent = totalVotes == 0
                    ? 0
                    : Math.Round((double)count / totalVotes * 100, 2);

                optionStats.Add(new OptionStatsDto
                {
                    Id = option.Id,
                    Count = count,
                    Percent = percent
                });
            }

            questionStats.Add(new QuestionStatsDto
            {
                Id = question.Id,
                Options = optionStats
            });
        }

        return new SurveyStatsDto
        {
            TotalVotes = totalVotes,
            Questions = questionStats
        };
    }
}
