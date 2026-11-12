using PollingSystem.Data.Enums;
using PollingSystem.Data.DTOs;
using Microsoft.EntityFrameworkCore;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Repositories;

public sealed class SurveyRepository(AppDbContext context) : ISurveyRepository
{
    public async Task<Survey> AddAsync(CreateSurveyDto dto, CancellationToken cancellationToken = default)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var survey = new Survey
        {
            AuthorId = dto.AuthorId,
            Title = dto.Title,
            Description = dto.Description,
            EndsAt = dto.EndsAt,
            Status = SurveyStatus.Draft
        };

        for (var questionPosition = 0; questionPosition < dto.Questions.Count; questionPosition++)
        {
            var questionDto = dto.Questions[questionPosition];
            var question = new Question
            {
                Survey = survey,
                SurveyId = survey.Id,
                Title = questionDto.Title,
                Type = questionDto.Type,
                IsRequired = questionDto.IsRequired,
                Position = questionPosition
            };

            for (var optionPosition = 0; optionPosition < questionDto.Options.Count; optionPosition++)
            {
                question.Options.Add(new QuestionOption
                {
                    Question = question,
                    QuestionId = question.Id,
                    Text = questionDto.Options[optionPosition].Text,
                    Position = optionPosition
                });
            }

            survey.Questions.Add(question);
        }

        await context.Surveys.AddAsync(survey, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return survey;
    }

    public Task<Survey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Surveys.AsNoTracking()
            .Include(survey => survey.Questions.OrderBy(question => question.Position))
            .ThenInclude(question => question.Options.OrderBy(option => option.Position))
            .SingleOrDefaultAsync(survey => survey.Id == id, cancellationToken);
    }
}
