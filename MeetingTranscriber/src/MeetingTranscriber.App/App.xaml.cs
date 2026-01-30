using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MeetingTranscriber.Services.Audio;
using MeetingTranscriber.Services.Settings;
using MeetingTranscriber.Services.Storage;
using MeetingTranscriber.Services.Summary;
using MeetingTranscriber.Services.Transcription;
using MeetingTranscriber.ViewModels;
using MeetingTranscriber.Views;

namespace MeetingTranscriber;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var configuration = BuildConfiguration();
        var services = new ServiceCollection();

        ConfigureServices(services, configuration);

        _serviceProvider = services.BuildServiceProvider();
        Services = _serviceProvider;

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.AddSingleton(configuration);
        services.Configure<AudioSettings>(configuration.GetSection("Audio"));

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Core Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<ITranscriptionService, DeepgramTranscriptionService>();
        services.AddSingleton<ISummaryService, ClaudeSummaryService>();
        services.AddSingleton<ISessionRepository, SessionRepository>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DeviceSelectorViewModel>();
        services.AddTransient<TranscriptViewModel>();
        services.AddTransient<SummaryViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SessionHistoryViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<SettingsWindow>();
        services.AddTransient<SessionHistoryWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

// Settings classes for appsettings.json (fallback only)
public class AudioSettings
{
    public int SampleRate { get; set; } = 16000;
    public int Channels { get; set; } = 1;
    public int BitsPerSample { get; set; } = 16;
}
