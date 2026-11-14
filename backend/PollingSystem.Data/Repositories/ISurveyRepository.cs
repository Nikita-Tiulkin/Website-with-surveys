
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Repositories;

public interface ISurveyRepository
{
    Task<Survey> AddAsync(
        CreateSurveyDto dto,
        CancellationToken cancellationToken = default);

    Task<Survey?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<List<Survey>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<List<Survey>> GetByAuthorIdAsync(
        Guid authorId,
        CancellationToken cancellationToken = default);

    Task<SurveyResponse> AddResponseAsync(
        Guid surveyId,
        Guid userId,
        SubmitVoteDto dto,
        CancellationToken cancellationToken = default);

    Task<SurveyStatsDto?> GetStatsAsync(
        Guid surveyId,
        CancellationToken cancellationToken = default);
}
