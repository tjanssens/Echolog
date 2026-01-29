using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using MeetingTranscriber.Models;

namespace MeetingTranscriber.ViewModels;

public partial class TranscriptViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<TranscriptSegment> _segments = new();

    [ObservableProperty]
    private bool _autoScroll = true;

    public event EventHandler? ScrollToEndRequested;

    public void AddSegment(TranscriptSegment segment)
    {
        // Must run on UI thread
        Application.Current.Dispatcher.Invoke(() =>
        {
            // If it's a final segment, replace any previous interim segment from same speaker
            if (segment.IsFinal)
            {
                var lastInterim = Segments.LastOrDefault(s => !s.IsFinal && s.Speaker == segment.Speaker);
                if (lastInterim != null)
                {
                    var index = Segments.IndexOf(lastInterim);
                    Segments[index] = segment;
                }
                else
                {
                    Segments.Add(segment);
                }
            }
            else
            {
                // For interim results, update or add
                var existing = Segments.LastOrDefault(s => !s.IsFinal && s.Speaker == segment.Speaker);
                if (existing != null)
                {
                    var index = Segments.IndexOf(existing);
                    Segments[index] = segment;
                }
                else
                {
                    Segments.Add(segment);
                }
            }

            if (AutoScroll)
            {
                ScrollToEndRequested?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    public void Clear()
    {
        Application.Current.Dispatcher.Invoke(() => Segments.Clear());
    }

    public string ToMarkdown(Session session)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Transcript - {session.StartTime:yyyy-MM-dd HH:mm}");
        sb.AppendLine();

        // Participants
        var speakers = Segments.Select(s => s.Speaker).Distinct().ToList();
        if (speakers.Any())
        {
            sb.AppendLine("## Deelnemers");
            foreach (var speaker in speakers)
            {
                var label = session.SpeakerLabels.TryGetValue(speaker, out var name) ? name : speaker;
                sb.AppendLine($"- {speaker}: {label}");
            }
            sb.AppendLine();
        }

        // Transcript
        sb.AppendLine("## Transcript");
        sb.AppendLine();

        foreach (var segment in Segments.Where(s => s.IsFinal))
        {
            var speakerLabel = session.SpeakerLabels.TryGetValue(segment.Speaker, out var name)
                ? name
                : segment.Speaker;

            sb.AppendLine($"**{segment.Timestamp:HH:mm:ss} - {speakerLabel}**");
            sb.AppendLine(segment.Text);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
