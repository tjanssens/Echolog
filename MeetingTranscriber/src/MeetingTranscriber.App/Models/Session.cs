namespace MeetingTranscriber.Models;

public class Session
{
    public Guid Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AudioInputPath { get; set; } = string.Empty;
    public string AudioOutputPath { get; set; } = string.Empty;
    public string AudioMixedPath { get; set; } = string.Empty;
    public List<TranscriptSegment> Segments { get; set; } = new();
    public string? Summary { get; set; }
    public Dictionary<string, string> SpeakerLabels { get; set; } = new();
}
