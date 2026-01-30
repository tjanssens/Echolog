using MeetingTranscriber.Models;

namespace MeetingTranscriber.Services.Storage;

public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session);
    Task<Session?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Session>> GetAllAsync();
    Task UpdateAsync(Session session);
    Task DeleteAsync(Guid id);
}
