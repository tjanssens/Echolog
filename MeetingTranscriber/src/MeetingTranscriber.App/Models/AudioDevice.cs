namespace MeetingTranscriber.Models;

public enum AudioDeviceType
{
    Input,
    Output
}

public record AudioDevice(
    string Id,
    string Name,
    AudioDeviceType Type
);
