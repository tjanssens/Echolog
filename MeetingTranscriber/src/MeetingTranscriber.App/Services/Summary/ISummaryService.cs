using MeetingTranscriber.Models;

namespace MeetingTranscriber.Services.Summary;

public interface ISummaryService
{
    Task<string> GenerateSummaryAsync(
        List<TranscriptSegment> segments,
        CancellationToken cancellationToken = default
    );
}
