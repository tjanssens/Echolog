using System.Text;
using MeetingTranscriber.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MeetingTranscriber.Services.Summary;

public class ClaudeSummaryService : ISummaryService
{
    private readonly ClaudeSettings _settings;
    private readonly ILogger<ClaudeSummaryService> _logger;

    private const string SummaryPrompt = """
        Je bent een assistent die vergaderverslagen maakt. Analyseer het volgende transcript en maak:

        1. **Samenvatting** (max 5 paragrafen)
           - Belangrijkste besproken onderwerpen
           - Genomen beslissingen

        2. **Actiepunten**
           - Wie moet wat doen
           - Eventuele deadlines genoemd

        3. **Vragen/Open punten**
           - Onbeantwoorde vragen
           - Onderwerpen die follow-up nodig hebben

        Transcript:
        {0}

        Geef het resultaat in Markdown formaat.
        """;

    public ClaudeSummaryService(
        IOptions<ClaudeSettings> settings,
        ILogger<ClaudeSummaryService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GenerateSummaryAsync(
        List<TranscriptSegment> segments,
        CancellationToken cancellationToken = default)
    {
        if (!segments.Any())
        {
            return "Geen transcript beschikbaar voor samenvatting.";
        }

        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            _logger.LogWarning("Claude API key is not configured");
            return "Claude API key is niet geconfigureerd. Configureer de API key in appsettings.json.";
        }

        // Build transcript text
        var transcriptBuilder = new StringBuilder();
        foreach (var segment in segments.Where(s => s.IsFinal))
        {
            transcriptBuilder.AppendLine($"[{segment.Timestamp:HH:mm:ss}] {segment.Speaker}: {segment.Text}");
        }

        var prompt = string.Format(SummaryPrompt, transcriptBuilder.ToString());

        _logger.LogInformation("Generating summary using model {Model}", _settings.Model);

        // Anthropic SDK implementation will be added in Phase 7
        // This will include:
        // - AnthropicClient initialization
        // - CreateMessageAsync with the prompt
        // - Streaming response handling

        // For now, return a placeholder
        await Task.Delay(100, cancellationToken); // Simulate API call

        return """
            ## Samenvatting

            *Samenvatting wordt gegenereerd wanneer de Claude API is geconfigureerd.*

            ## Actiepunten

            - Configureer de Claude API key in appsettings.json

            ## Open punten

            - Implementatie volgt in Fase 7
            """;
    }
}
