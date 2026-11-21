
using PollingSystem.Api.Models;
using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;

namespace PollingSystem.Api.Services.Interfaces;

public interface ISurveyService
{
    Task<List<SurveyDto>> GetAllAsync(
        Guid? authorId,
        CancellationToken cancellationToken = default);

    Task<SurveyDto?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SurveyDto> CreateAsync(
        Guid authorId,
        CreateSurveyRequest request,
        CancellationToken cancellationToken = default);

    Task<SurveyResponse> VoteAsync(
        Guid surveyId,
        Guid userId,
        SubmitVoteDto request,
        CancellationToken cancellationToken = default);

    Task<SurveyStatsDto?> GetStatsAsync(
        Guid surveyId,
        CancellationToken cancellationToken = default);
}
