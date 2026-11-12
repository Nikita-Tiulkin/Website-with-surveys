using PollingSystem.Data.DTOs;
using PollingSystem.Data.Entities;

namespace PollingSystem.Data.Repositories;

public interface ISurveyRepository
{
    Task<Survey> AddAsync(CreateSurveyDto dto, CancellationToken cancellationToken = default);
    Task<Survey?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
