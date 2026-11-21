
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
        ArgumentNullException.ThrowIfNull(dto);

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
            .SingleOrDefaultAsync(
                survey => survey.Id == id,
                cancellationToken);
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

    public async Task AddResponseAsync(
        SurveyResponse response,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        await context.SurveyResponses.AddAsync(response, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SurveyStatsDto?> GetStatsAsync(
        Guid surveyId,
        CancellationToken cancellationToken = default)
    {
        Survey? survey = await context.Surveys
            .AsNoTracking()
            .Include(item => item.Questions)
                .ThenInclude(question => question.Options)
            .SingleOrDefaultAsync(
                item => item.Id == surveyId,
                cancellationToken);

        if (survey is null)
        {
            return null;
        }

        int totalVotes = await context.SurveyResponses
            .CountAsync(
                response => response.SurveyId == surveyId,
                cancellationToken);

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

    public Task<bool> HasUserVotedAsync(
        Guid surveyId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return context.SurveyResponses
            .AsNoTracking()
            .AnyAsync(
                response => response.SurveyId == surveyId &&
                            response.UserId == userId,
                cancellationToken);
    }
}
