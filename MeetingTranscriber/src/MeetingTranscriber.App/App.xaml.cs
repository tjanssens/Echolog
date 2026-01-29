using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MeetingTranscriber.Services.Audio;
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
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Configuration
        services.AddSingleton(configuration);
        services.Configure<DeepgramSettings>(configuration.GetSection("Deepgram"));
        services.Configure<ClaudeSettings>(configuration.GetSection("Claude"));
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.Configure<AudioSettings>(configuration.GetSection("Audio"));

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Services
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<ITranscriptionService, DeepgramTranscriptionService>();
        services.AddSingleton<ISummaryService, ClaudeSummaryService>();
        services.AddSingleton<ISessionRepository, SessionRepository>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DeviceSelectorViewModel>();
        services.AddTransient<TranscriptViewModel>();
        services.AddTransient<SummaryViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

// Settings classes
public class DeepgramSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Language { get; set; } = "nl";
    public string Model { get; set; } = "nova-2";
}

public class ClaudeSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "claude-sonnet-4-20250514";
}

public class StorageSettings
{
    public string BasePath { get; set; } = "%APPDATA%/MeetingTranscriber";

    public string GetExpandedPath()
    {
        return Environment.ExpandEnvironmentVariables(BasePath);
    }
}

public class AudioSettings
{
    public int SampleRate { get; set; } = 16000;
    public int Channels { get; set; } = 1;
    public int BitsPerSample { get; set; } = 16;
}
