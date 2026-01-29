using FluentAssertions;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MeetingTranscriber.Tests.Services;

public class SessionRepositoryTests : IDisposable
{
    private readonly Mock<IOptions<StorageSettings>> _settingsMock;
    private readonly Mock<ILogger<SessionRepository>> _loggerMock;
    private readonly SessionRepository _repository;
    private readonly string _testBasePath;

    public SessionRepositoryTests()
    {
        _testBasePath = Path.Combine(Path.GetTempPath(), $"MeetingTranscriberTests_{Guid.NewGuid()}");

        _settingsMock = new Mock<IOptions<StorageSettings>>();
        _settingsMock.Setup(x => x.Value).Returns(new StorageSettings
        {
            BasePath = _testBasePath
        });

        _loggerMock = new Mock<ILogger<SessionRepository>>();

        _repository = new SessionRepository(_settingsMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _repository.Dispose();

        if (Directory.Exists(_testBasePath))
        {
            Directory.Delete(_testBasePath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldSaveSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sessionPath = Path.Combine(_testBasePath, "sessions", sessionId.ToString());
        Directory.CreateDirectory(sessionPath);

        var session = new Session
        {
            Id = sessionId,
            StartTime = DateTime.Now,
            Title = "Test Session",
            AudioInputPath = Path.Combine(sessionPath, "audio_input.wav")
        };

        // Act
        var result = await _repository.CreateAsync(session);

        // Assert
        result.Should().Be(session);
        File.Exists(Path.Combine(sessionPath, "session.json")).Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingSession_ShouldReturnSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sessionPath = Path.Combine(_testBasePath, "sessions", sessionId.ToString());
        Directory.CreateDirectory(sessionPath);

        var session = new Session
        {
            Id = sessionId,
            StartTime = DateTime.Now,
            Title = "Test Session",
            AudioInputPath = Path.Combine(sessionPath, "audio_input.wav")
        };

        await _repository.CreateAsync(session);

        // Act
        var result = await _repository.GetByIdAsync(sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sessionId);
        result.Title.Should().Be("Test Session");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingSession_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithNoSessions_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithSessions_ShouldReturnAllSessions()
    {
        // Arrange
        for (int i = 0; i < 3; i++)
        {
            var sessionId = Guid.NewGuid();
            var sessionPath = Path.Combine(_testBasePath, "sessions", sessionId.ToString());
            Directory.CreateDirectory(sessionPath);

            var session = new Session
            {
                Id = sessionId,
                StartTime = DateTime.Now.AddMinutes(-i),
                Title = $"Session {i}",
                AudioInputPath = Path.Combine(sessionPath, "audio_input.wav")
            };

            await _repository.CreateAsync(session);
        }

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnSessionsOrderedByStartTimeDescending()
    {
        // Arrange
        var sessions = new List<Session>();
        for (int i = 0; i < 3; i++)
        {
            var sessionId = Guid.NewGuid();
            var sessionPath = Path.Combine(_testBasePath, "sessions", sessionId.ToString());
            Directory.CreateDirectory(sessionPath);

            var session = new Session
            {
                Id = sessionId,
                StartTime = DateTime.Now.AddDays(-i),
                Title = $"Session {i}",
                AudioInputPath = Path.Combine(sessionPath, "audio_input.wav")
            };

            await _repository.CreateAsync(session);
            sessions.Add(session);
        }

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().BeInDescendingOrder(s => s.StartTime);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sessionPath = Path.Combine(_testBasePath, "sessions", sessionId.ToString());
        Directory.CreateDirectory(sessionPath);

        var session = new Session
        {
            Id = sessionId,
            StartTime = DateTime.Now,
            Title = "Original Title",
            AudioInputPath = Path.Combine(sessionPath, "audio_input.wav")
        };

        await _repository.CreateAsync(session);

        // Act
        session.Title = "Updated Title";
        session.Summary = "New summary";
        await _repository.UpdateAsync(session);

        // Assert
        var result = await _repository.GetByIdAsync(sessionId);
        result!.Title.Should().Be("Updated Title");
        result.Summary.Should().Be("New summary");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var sessionPath = Path.Combine(_testBasePath, "sessions", sessionId.ToString());
        Directory.CreateDirectory(sessionPath);

        var session = new Session
        {
            Id = sessionId,
            StartTime = DateTime.Now,
            Title = "To Delete",
            AudioInputPath = Path.Combine(sessionPath, "audio_input.wav")
        };

        await _repository.CreateAsync(session);

        // Act
        await _repository.DeleteAsync(sessionId);

        // Assert
        Directory.Exists(sessionPath).Should().BeFalse();
        var result = await _repository.GetByIdAsync(sessionId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingSession_ShouldNotThrow()
    {
        // Act
        var action = async () => await _repository.DeleteAsync(Guid.NewGuid());

        // Assert
        await action.Should().NotThrowAsync();
    }
}
