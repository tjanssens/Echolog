using System.Net.Http;
using System.Text;
using System.Text.Json;
using MeetingTranscriber.Models;
using MeetingTranscriber.Services.Settings;
using Microsoft.Extensions.Logging;

namespace MeetingTranscriber.Services.Summary;

public class ClaudeSummaryService : ISummaryService
{
    private readonly ISettingsService _settingsService;
    private readonly ILogger<ClaudeSummaryService> _logger;
    private readonly HttpClient _httpClient;

    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";

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
        ISettingsService settingsService,
        ILogger<ClaudeSummaryService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateSummaryAsync(
        List<TranscriptSegment> segments,
        CancellationToken cancellationToken = default)
    {
        if (!segments.Any())
        {
            return "Geen transcript beschikbaar voor samenvatting.";
        }

        var settings = await _settingsService.GetSettingsAsync();

        if (string.IsNullOrEmpty(settings.ClaudeApiKey))
        {
            _logger.LogWarning("Claude API key is not configured");
            return "Claude API key is niet geconfigureerd. Ga naar Instellingen om de API key in te voeren.";
        }

        // Build transcript text
        var transcriptBuilder = new StringBuilder();
        foreach (var segment in segments.Where(s => s.IsFinal))
        {
            transcriptBuilder.AppendLine($"[{segment.Timestamp:HH:mm:ss}] {segment.Speaker}: {segment.Text}");
        }

        var prompt = string.Format(SummaryPrompt, transcriptBuilder.ToString());

        _logger.LogInformation("Generating summary using model {Model}", settings.ClaudeModel);

        try
        {
            var requestBody = new
            {
                model = settings.ClaudeModel,
                max_tokens = 4096,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl);
            request.Headers.Add("x-api-key", settings.ClaudeApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Claude API error: {StatusCode} - {Error}", response.StatusCode, errorContent);

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return "Claude API key is ongeldig. Controleer de API key in Instellingen.";
                }

                return $"Fout bij genereren samenvatting: {response.StatusCode}";
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseJson = JsonDocument.Parse(responseContent);

            // Extract the text from the response
            var content = responseJson.RootElement
                .GetProperty("content")
                .EnumerateArray()
                .FirstOrDefault();

            if (content.ValueKind != JsonValueKind.Undefined &&
                content.TryGetProperty("text", out var textElement))
            {
                var summaryText = textElement.GetString();
                _logger.LogInformation("Summary generated successfully");
                return summaryText ?? "Geen samenvatting ontvangen.";
            }

            return "Onverwacht antwoord van Claude API.";
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Summary generation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate summary");
            return $"Fout bij genereren samenvatting: {ex.Message}";
        }
    }
}
