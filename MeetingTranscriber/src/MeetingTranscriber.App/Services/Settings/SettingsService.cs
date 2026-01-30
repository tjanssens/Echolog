using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MeetingTranscriber.Services.Settings;

public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsFilePath;
    private readonly HttpClient _httpClient;
    private AppSettings? _cachedSettings;
    private readonly object _lockObject = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDir = Path.Combine(appDataPath, "MeetingTranscriber");
        Directory.CreateDirectory(settingsDir);

        _settingsFilePath = Path.Combine(settingsDir, "settings.json");
    }

    public async Task<AppSettings> GetSettingsAsync()
    {
        lock (_lockObject)
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }
        }

        AppSettings settings;

        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                _logger.LogDebug("Settings loaded from {Path}", _settingsFilePath);
            }
            else
            {
                settings = CreateDefaultSettings();
                await SaveSettingsAsync(settings);
                _logger.LogInformation("Created default settings at {Path}", _settingsFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            settings = CreateDefaultSettings();
        }

        // Ensure storage path is set
        if (string.IsNullOrEmpty(settings.StoragePath))
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            settings.StoragePath = Path.Combine(appDataPath, "MeetingTranscriber");
        }

        lock (_lockObject)
        {
            _cachedSettings = settings;
        }

        return settings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            lock (_lockObject)
            {
                _cachedSettings = settings;
            }

            _logger.LogInformation("Settings saved to {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    public async Task<bool> ValidateDeepgramApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.deepgram.com/v1/projects");
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", apiKey);

            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Deepgram API key");
            return false;
        }
    }

    public async Task<bool> ValidateClaudeApiKeyAsync(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = "claude-sonnet-4-20250514",
                    max_tokens = 1,
                    messages = new[] { new { role = "user", content = "Hi" } }
                }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            // Even if we get a 400 (bad request), it means the API key is valid
            // 401 means invalid API key
            return response.StatusCode != System.Net.HttpStatusCode.Unauthorized;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to validate Claude API key");
            return false;
        }
    }

    private AppSettings CreateDefaultSettings()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        return new AppSettings
        {
            DeepgramApiKey = string.Empty,
            DeepgramLanguage = "nl",
            DeepgramModel = "nova-2",
            ClaudeApiKey = string.Empty,
            ClaudeModel = "claude-sonnet-4-20250514",
            StoragePath = Path.Combine(appDataPath, "MeetingTranscriber"),
            AudioSampleRate = 16000,
            AudioChannels = 1,
            AudioBitsPerSample = 16,
            AutoScroll = true,
            DarkTheme = true
        };
    }
}
