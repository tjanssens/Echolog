using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingTranscriber.Services.Settings;
using Microsoft.Extensions.Logging;

namespace MeetingTranscriber.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty]
    private string _deepgramApiKey = string.Empty;

    [ObservableProperty]
    private string _deepgramLanguage = "nl";

    [ObservableProperty]
    private string _deepgramModel = "nova-2";

    [ObservableProperty]
    private string _claudeApiKey = string.Empty;

    [ObservableProperty]
    private string _claudeModel = "claude-sonnet-4-20250514";

    [ObservableProperty]
    private string _storagePath = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _darkTheme = true;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isValidatingDeepgram;

    [ObservableProperty]
    private bool _isValidatingClaude;

    [ObservableProperty]
    private bool? _isDeepgramValid;

    [ObservableProperty]
    private bool? _isClaudeValid;

    [ObservableProperty]
    private bool _hasChanges;

    public SettingsViewModel(
        ISettingsService settingsService,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsService.GetSettingsAsync();

            DeepgramApiKey = settings.DeepgramApiKey;
            DeepgramLanguage = settings.DeepgramLanguage;
            DeepgramModel = settings.DeepgramModel;
            ClaudeApiKey = settings.ClaudeApiKey;
            ClaudeModel = settings.ClaudeModel;
            StoragePath = settings.StoragePath;
            AutoScroll = settings.AutoScroll;
            DarkTheme = settings.DarkTheme;

            HasChanges = false;
            StatusMessage = "Instellingen geladen";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            StatusMessage = "Fout bij laden instellingen";
        }
    }

    partial void OnDeepgramApiKeyChanged(string value) => MarkChanged();
    partial void OnDeepgramLanguageChanged(string value) => MarkChanged();
    partial void OnDeepgramModelChanged(string value) => MarkChanged();
    partial void OnClaudeApiKeyChanged(string value) => MarkChanged();
    partial void OnClaudeModelChanged(string value) => MarkChanged();
    partial void OnStoragePathChanged(string value) => MarkChanged();
    partial void OnAutoScrollChanged(bool value) => MarkChanged();
    partial void OnDarkThemeChanged(bool value) => MarkChanged();

    private void MarkChanged()
    {
        HasChanges = true;
        IsDeepgramValid = null;
        IsClaudeValid = null;
    }

    [RelayCommand]
    private async Task ValidateDeepgramAsync()
    {
        if (string.IsNullOrWhiteSpace(DeepgramApiKey))
        {
            IsDeepgramValid = false;
            StatusMessage = "Voer eerst een Deepgram API key in";
            return;
        }

        IsValidatingDeepgram = true;
        StatusMessage = "Deepgram API key valideren...";

        try
        {
            IsDeepgramValid = await _settingsService.ValidateDeepgramApiKeyAsync(DeepgramApiKey);
            StatusMessage = IsDeepgramValid == true
                ? "Deepgram API key is geldig!"
                : "Deepgram API key is ongeldig";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Deepgram API key");
            IsDeepgramValid = false;
            StatusMessage = "Fout bij valideren Deepgram API key";
        }
        finally
        {
            IsValidatingDeepgram = false;
        }
    }

    [RelayCommand]
    private async Task ValidateClaudeAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaudeApiKey))
        {
            IsClaudeValid = false;
            StatusMessage = "Voer eerst een Claude API key in";
            return;
        }

        IsValidatingClaude = true;
        StatusMessage = "Claude API key valideren...";

        try
        {
            IsClaudeValid = await _settingsService.ValidateClaudeApiKeyAsync(ClaudeApiKey);
            StatusMessage = IsClaudeValid == true
                ? "Claude API key is geldig!"
                : "Claude API key is ongeldig";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Claude API key");
            IsClaudeValid = false;
            StatusMessage = "Fout bij valideren Claude API key";
        }
        finally
        {
            IsValidatingClaude = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var settings = new AppSettings
            {
                DeepgramApiKey = DeepgramApiKey,
                DeepgramLanguage = DeepgramLanguage,
                DeepgramModel = DeepgramModel,
                ClaudeApiKey = ClaudeApiKey,
                ClaudeModel = ClaudeModel,
                StoragePath = StoragePath,
                AutoScroll = AutoScroll,
                DarkTheme = DarkTheme
            };

            await _settingsService.SaveSettingsAsync(settings);

            HasChanges = false;
            StatusMessage = "Instellingen opgeslagen!";
            _logger.LogInformation("Settings saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            StatusMessage = "Fout bij opslaan instellingen";
        }
    }

    [RelayCommand]
    private void BrowseStoragePath()
    {
        // This will be handled in the view with a FolderBrowserDialog
    }
}
