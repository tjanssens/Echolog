using System.IO;
using LiteDB;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Settings;
using Microsoft.Extensions.Logging;

namespace MeetingTranscriber.Services.Storage;

public class SessionRepository : ISessionRepository, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SessionRepository> _logger;
    private LiteDatabase? _database;
    private ILiteCollection<SessionEntity>? _collection;
    private bool _disposed;
    private readonly object _lockObject = new();

    public SessionRepository(
        ISettingsService settingsService,
        ILogger<SessionRepository> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_database != null) return;

        lock (_lockObject)
        {
            if (_database != null) return;

            var settings = _settingsService.GetSettingsAsync().GetAwaiter().GetResult();
            var basePath = settings.StoragePath;

            if (string.IsNullOrEmpty(basePath))
            {
                basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MeetingTranscriber"
                );
            }

            Directory.CreateDirectory(basePath);
            var dbPath = Path.Combine(basePath, "sessions.db");

            _database = new LiteDatabase($"Filename={dbPath};Connection=shared");
            _collection = _database.GetCollection<SessionEntity>("sessions");

            // Create indexes
            _collection.EnsureIndex(x => x.StartTime);

            _logger.LogDebug("Session repository initialized at {DbPath}", dbPath);
        }
    }

    public async Task<Session> CreateAsync(Session session)
    {
        await EnsureInitializedAsync();

        var entity = SessionEntity.FromSession(session);
        _collection!.Insert(entity);

        // Also save JSON file in session directory for easy access
        await SaveSessionJsonAsync(session);

        _logger.LogDebug("Session {SessionId} created", session.Id);
        return session;
    }

    public async Task<Session?> GetByIdAsync(Guid id)
    {
        await EnsureInitializedAsync();

        var entity = _collection!.FindById(id);
        return entity?.ToSession();
    }

    public async Task<IReadOnlyList<Session>> GetAllAsync()
    {
        await EnsureInitializedAsync();

        var entities = _collection!
            .FindAll()
            .OrderByDescending(e => e.StartTime)
            .ToList();

        return entities.Select(e => e.ToSession()).ToList();
    }

    public async Task UpdateAsync(Session session)
    {
        await EnsureInitializedAsync();

        var entity = SessionEntity.FromSession(session);
        _collection!.Update(entity);

        // Also update JSON file
        await SaveSessionJsonAsync(session);

        _logger.LogDebug("Session {SessionId} updated", session.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await EnsureInitializedAsync();

        _collection!.Delete(id);

        // Also delete session directory
        var settings = await _settingsService.GetSettingsAsync();
        var sessionPath = Path.Combine(settings.StoragePath, "sessions", id.ToString());

        if (Directory.Exists(sessionPath))
        {
            try
            {
                Directory.Delete(sessionPath, recursive: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete session directory {Path}", sessionPath);
            }
        }

        _logger.LogInformation("Deleted session {SessionId}", id);
    }

    private async Task SaveSessionJsonAsync(Session session)
    {
        try
        {
            var sessionPath = Path.GetDirectoryName(session.AudioInputPath);
            if (!string.IsNullOrEmpty(sessionPath) && Directory.Exists(sessionPath))
            {
                var jsonPath = Path.Combine(sessionPath, "session.json");
                var json = System.Text.Json.JsonSerializer.Serialize(session, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await File.WriteAllTextAsync(jsonPath, json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save session JSON for {SessionId}", session.Id);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _database?.Dispose();
        _database = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

// Internal entity class for LiteDB storage
internal class SessionEntity
{
    [BsonId]
    public Guid Id { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AudioInputPath { get; set; } = string.Empty;
    public string AudioOutputPath { get; set; } = string.Empty;
    public string AudioMixedPath { get; set; } = string.Empty;
    public List<TranscriptSegmentEntity> Segments { get; set; } = new();
    public string? Summary { get; set; }
    public Dictionary<string, string> SpeakerLabels { get; set; } = new();

    public static SessionEntity FromSession(Session session)
    {
        return new SessionEntity
        {
            Id = session.Id,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            Title = session.Title,
            AudioInputPath = session.AudioInputPath,
            AudioOutputPath = session.AudioOutputPath,
            AudioMixedPath = session.AudioMixedPath,
            Segments = session.Segments.Select(TranscriptSegmentEntity.FromSegment).ToList(),
            Summary = session.Summary,
            SpeakerLabels = session.SpeakerLabels
        };
    }

    public Session ToSession()
    {
        return new Session
        {
            Id = Id,
            StartTime = StartTime,
            EndTime = EndTime,
            Title = Title,
            AudioInputPath = AudioInputPath,
            AudioOutputPath = AudioOutputPath,
            AudioMixedPath = AudioMixedPath,
            Segments = Segments.Select(s => s.ToSegment()).ToList(),
            Summary = Summary,
            SpeakerLabels = SpeakerLabels
        };
    }
}

internal class TranscriptSegmentEntity
{
    public DateTime Timestamp { get; set; }
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool IsFinal { get; set; }
    public int Source { get; set; }

    public static TranscriptSegmentEntity FromSegment(TranscriptSegment segment)
    {
        return new TranscriptSegmentEntity
        {
            Timestamp = segment.Timestamp,
            Speaker = segment.Speaker,
            Text = segment.Text,
            IsFinal = segment.IsFinal,
            Source = (int)segment.Source
        };
    }

    public TranscriptSegment ToSegment()
    {
        return new TranscriptSegment(
            Timestamp,
            Speaker,
            Text,
            IsFinal,
            (AudioSource)Source
        );
    }
}
