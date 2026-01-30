namespace MeetingTranscriber.Services.Settings;

public interface ISettingsService
{
    Task<AppSettings> GetSettingsAsync();
    Task SaveSettingsAsync(AppSettings settings);
    Task<bool> ValidateDeepgramApiKeyAsync(string apiKey);
    Task<bool> ValidateClaudeApiKeyAsync(string apiKey);
}

public class AppSettings
{
    public string DeepgramApiKey { get; set; } = string.Empty;
    public string DeepgramLanguage { get; set; } = "nl";
    public string DeepgramModel { get; set; } = "nova-2";

    public string ClaudeApiKey { get; set; } = string.Empty;
    public string ClaudeModel { get; set; } = "claude-sonnet-4-20250514";

    public string StoragePath { get; set; } = string.Empty;
    public int AudioSampleRate { get; set; } = 16000;
    public int AudioChannels { get; set; } = 1;
    public int AudioBitsPerSample { get; set; } = 16;

    public bool AutoScroll { get; set; } = true;
    public bool DarkTheme { get; set; } = true;
}
