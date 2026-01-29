using System.Text.Json;
using MeetingTranscriber.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeetingTranscriber.Services.Storage;

public class SessionRepository : ISessionRepository, IDisposable
{
    private readonly StorageSettings _settings;
    private readonly ILogger<SessionRepository> _logger;
    private readonly string _dbPath;
    private bool _disposed;

    public SessionRepository(
        IOptions<StorageSettings> settings,
        ILogger<SessionRepository> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        var basePath = _settings.GetExpandedPath();
        Directory.CreateDirectory(basePath);
        _dbPath = Path.Combine(basePath, "sessions.db");

        _logger.LogDebug("Session repository initialized at {DbPath}", _dbPath);
    }

    public async Task<Session> CreateAsync(Session session)
    {
        // LiteDB implementation will be added in Phase 6
        // For now, save as JSON file in session directory

        var sessionPath = Path.GetDirectoryName(session.AudioInputPath);
        if (!string.IsNullOrEmpty(sessionPath))
        {
            var jsonPath = Path.Combine(sessionPath, "session.json");
            var json = JsonSerializer.Serialize(session, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(jsonPath, json);
            _logger.LogDebug("Session {SessionId} saved to {Path}", session.Id, jsonPath);
        }

        return session;
    }

    public async Task<Session?> GetByIdAsync(Guid id)
    {
        // LiteDB implementation will be added in Phase 6
        var basePath = _settings.GetExpandedPath();
        var sessionPath = Path.Combine(basePath, "sessions", id.ToString(), "session.json");

        if (File.Exists(sessionPath))
        {
            var json = await File.ReadAllTextAsync(sessionPath);
            return JsonSerializer.Deserialize<Session>(json);
        }

        return null;
    }

    public async Task<IReadOnlyList<Session>> GetAllAsync()
    {
        var sessions = new List<Session>();
        var basePath = _settings.GetExpandedPath();
        var sessionsDir = Path.Combine(basePath, "sessions");

        if (!Directory.Exists(sessionsDir))
        {
            return sessions;
        }

        foreach (var dir in Directory.GetDirectories(sessionsDir))
        {
            var jsonPath = Path.Combine(dir, "session.json");
            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(jsonPath);
                    var session = JsonSerializer.Deserialize<Session>(json);
                    if (session != null)
                    {
                        sessions.Add(session);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load session from {Path}", jsonPath);
                }
            }
        }

        return sessions.OrderByDescending(s => s.StartTime).ToList();
    }

    public async Task UpdateAsync(Session session)
    {
        await CreateAsync(session); // Same logic for now
    }

    public Task DeleteAsync(Guid id)
    {
        var basePath = _settings.GetExpandedPath();
        var sessionPath = Path.Combine(basePath, "sessions", id.ToString());

        if (Directory.Exists(sessionPath))
        {
            Directory.Delete(sessionPath, recursive: true);
            _logger.LogInformation("Deleted session {SessionId}", id);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        // Dispose LiteDB connection in Phase 6
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
