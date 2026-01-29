# Meeting Transcriber - Claude Code Briefing

## Project Overzicht

Bouw een WPF desktop applicatie voor Windows die live vergaderingen transcribeert met ondersteuning voor meerdere audio-apparaten, speaker diarization, en automatische samenvattingen via Claude.

## Kernfunctionaliteit

### Must-Have Features

1. **Audio Device Selectie**
   - Gebruiker kan input device kiezen (microfoon)
   - Gebruiker kan output device kiezen (systeemaudio/speakers)
   - Beide apparaten worden simultaan opgenomen

2. **Live Opname & Transcriptie**
   - Start/stop/pauze controls
   - Realtime audio capture van beide bronnen
   - Audio wordt gestreamd naar Deepgram voor live transcriptie
   - Transcriptie verschijnt live op scherm met auto-scroll

3. **Speaker Diarization**
   - Automatische detectie van verschillende sprekers
   - Weergave van speaker labels in transcript (Speaker 1, Speaker 2, etc.)
   - Mogelijkheid om later namen toe te wijzen aan speakers

4. **Audio Opslag**
   - Input audio opslaan als WAV (microfoon)
   - Output audio opslaan als WAV (systeemaudio)
   - Gemixte audio opslaan als WAV

5. **Export & Samenvatting**
   - Transcript exporteren als Markdown
   - On-demand samenvatting genereren via Claude API
   - Samenvatting met actiepunten in Markdown formaat

### Nice-to-Have Features

- Sessie geschiedenis met zoekfunctie
- Settings scherm voor API keys
- Audio level meters tijdens opname

## Technologie Stack

| Component | Technologie | Versie |
|-----------|-------------|--------|
| Framework | .NET | 8.0 |
| UI | WPF | - |
| MVVM | CommunityToolkit.Mvvm | 8.x |
| UI Styling | MaterialDesignThemes | 5.x |
| Audio | NAudio | 2.2.1 |
| Transcriptie | Deepgram SDK | 4.x |
| AI Summary | Anthropic.SDK | latest |
| Storage | LiteDB | 5.x |
| DI | Microsoft.Extensions.DependencyInjection | 8.x |

## Architectuur

### MVVM Pattern

```
Views (XAML) <--binding--> ViewModels <---> Services <---> External APIs/Storage
```

### Projectstructuur

```
MeetingTranscriber/
├── MeetingTranscriber.sln
└── src/
    └── MeetingTranscriber.App/
        ├── App.xaml
        ├── App.xaml.cs
        ├── appsettings.json
        ├── Views/
        │   ├── MainWindow.xaml
        │   ├── DeviceSelectorView.xaml
        │   ├── TranscriptView.xaml
        │   ├── SummaryView.xaml
        │   └── SessionHistoryView.xaml
        ├── ViewModels/
        │   ├── MainViewModel.cs
        │   ├── DeviceSelectorViewModel.cs
        │   ├── TranscriptViewModel.cs
        │   └── SummaryViewModel.cs
        ├── Models/
        │   ├── AudioDevice.cs
        │   ├── TranscriptSegment.cs
        │   └── Session.cs
        ├── Services/
        │   ├── Audio/
        │   │   ├── IAudioCaptureService.cs
        │   │   └── AudioCaptureService.cs
        │   ├── Transcription/
        │   │   ├── ITranscriptionService.cs
        │   │   └── DeepgramTranscriptionService.cs
        │   ├── Summary/
        │   │   ├── ISummaryService.cs
        │   │   └── ClaudeSummaryService.cs
        │   └── Storage/
        │       ├── ISessionRepository.cs
        │       └── SessionRepository.cs
        ├── Converters/
        └── Resources/
            └── Styles.xaml
```

## Models

### AudioDevice.cs

```csharp
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
```

### TranscriptSegment.cs

```csharp
public enum AudioSource
{
    Microphone,
    SystemAudio
}

public record TranscriptSegment(
    DateTime Timestamp,
    string Speaker,
    string Text,
    bool IsFinal,
    AudioSource Source
);
```

### Session.cs

```csharp
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
```

## Service Interfaces

### IAudioCaptureService.cs

```csharp
public interface IAudioCaptureService
{
    IReadOnlyList<AudioDevice> GetInputDevices();
    IReadOnlyList<AudioDevice> GetOutputDevices();
    
    void SetInputDevice(string deviceId);
    void SetOutputDevice(string deviceId);
    
    Task StartCaptureAsync(string sessionPath);
    Task StopCaptureAsync();
    void PauseCapture();
    void ResumeCapture();
    
    event EventHandler<byte[]> AudioDataAvailable;
    event EventHandler<float> InputLevelChanged;
    event EventHandler<float> OutputLevelChanged;
}
```

### ITranscriptionService.cs

```csharp
public interface ITranscriptionService
{
    Task ConnectAsync();
    Task DisconnectAsync();
    Task SendAudioAsync(byte[] audioData);
    
    event EventHandler<TranscriptSegment> TranscriptReceived;
    event EventHandler<string> ErrorOccurred;
}
```

### ISummaryService.cs

```csharp
public interface ISummaryService
{
    Task<string> GenerateSummaryAsync(
        List<TranscriptSegment> segments,
        CancellationToken cancellationToken = default
    );
}
```

### ISessionRepository.cs

```csharp
public interface ISessionRepository
{
    Task<Session> CreateAsync(Session session);
    Task<Session?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Session>> GetAllAsync();
    Task UpdateAsync(Session session);
    Task DeleteAsync(Guid id);
}
```

## Audio Capture Implementatie Details

### NAudio Setup

**Microphone Capture:**
```csharp
// Gebruik WaveInEvent voor microphone input
var waveIn = new WaveInEvent
{
    DeviceNumber = deviceIndex,
    WaveFormat = new WaveFormat(16000, 16, 1) // 16kHz, 16-bit, mono voor Deepgram
};
```

**System Audio Capture (Loopback):**
```csharp
// Gebruik WasapiLoopbackCapture voor systeemaudio
var loopback = new WasapiLoopbackCapture(device);
// Let op: output is meestal 48kHz stereo, moet geresampeld worden naar 16kHz mono
```

**Resampling voor Deepgram:**
```csharp
// Deepgram verwacht: 16kHz, 16-bit, mono, linear PCM
var resampler = new MediaFoundationResampler(sourceProvider, new WaveFormat(16000, 16, 1));
```

### Audio Flow

```
[Microfoon 48kHz] ──► [Resample 16kHz] ──┬──► [WAV Writer]
                                         │
                                         ├──► [Mixer] ──► [Deepgram WebSocket]
                                         │
[Loopback 48kHz] ──► [Resample 16kHz] ──┴──► [WAV Writer]
```

## Deepgram Configuratie

### Connection Settings

```csharp
var options = new LiveTranscriptionOptions
{
    Model = "nova-2",
    Language = "nl",
    SmartFormat = true,
    Diarize = true,
    Punctuate = true,
    Encoding = "linear16",
    SampleRate = 16000,
    Channels = 1
};
```

### WebSocket Events

- `TranscriptReceived` - Ontvang transcript chunks
- `UtteranceEnd` - Einde van een utterance (speaker wissel)
- `Error` - Foutafhandeling
- `Close` - Connectie gesloten

## Claude Samenvatting Prompt

```
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
{transcript}

Geef het resultaat in Markdown formaat.
```

## UI Specificaties

### MainWindow Layout

```
┌─────────────────────────────────────────────────────────────┐
│  Meeting Transcriber                              [─] [□] [×]│
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────┐  ┌─────────────────────────┐   │
│  │ 🎤 Microfoon:           │  │ 🔊 Systeemaudio:        │   │
│  │ [ComboBox           ▼]  │  │ [ComboBox           ▼]  │   │
│  │ [████████░░] -12dB      │  │ [██████░░░░] -18dB      │   │
│  └─────────────────────────┘  └─────────────────────────┘   │
│                                                              │
│  [ ● Start ]  [ ⏸ Pauze ]  [ ⏹ Stop ]     🔴 00:15:32      │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│  Live Transcript                                             │
├─────────────────────────────────────────────────────────────┤
│  │                                                          │
│  │ 10:32:15  Speaker 1                                     │
│  │ Goedemorgen allemaal, laten we beginnen.                │
│  │                                                          │
│  │ 10:32:22  Speaker 2                                     │
│  │ Ik heb de API afgerond, vandaag focus ik op tests.     │
│  │                                                     ▼    │
├─────────────────────────────────────────────────────────────┤
│  [ 🤖 Genereer Samenvatting ]  [ 📄 Export MD ]  [ 📂 Hist ]│
└─────────────────────────────────────────────────────────────┘
```

### Styling

- Gebruik MaterialDesignThemes voor moderne look
- Dark theme als default
- Primaire kleur: Teal of Deep Purple
- Accent kleur: Amber

## Configuratie

### appsettings.json

```json
{
  "Deepgram": {
    "ApiKey": "",
    "Language": "nl",
    "Model": "nova-2"
  },
  "Claude": {
    "ApiKey": "",
    "Model": "claude-sonnet-4-20250514"
  },
  "Storage": {
    "BasePath": "%APPDATA%/MeetingTranscriber"
  },
  "Audio": {
    "SampleRate": 16000,
    "Channels": 1,
    "BitsPerSample": 16
  }
}
```

## Output Bestanden

Per sessie worden de volgende bestanden opgeslagen:

```
%APPDATA%/MeetingTranscriber/
└── sessions/
    └── {session-id}/
        ├── audio_input.wav      # Microfoon opname
        ├── audio_output.wav     # Systeemaudio opname
        ├── audio_mixed.wav      # Gecombineerde audio
        ├── transcript.md        # Ruwe transcriptie
        ├── summary.md           # Claude samenvatting (indien gegenereerd)
        └── session.json         # Metadata + speaker labels
```

### Transcript Markdown Format

```markdown
# Transcript - {datum} {tijd}

## Deelnemers
- Speaker 1: {naam indien toegewezen}
- Speaker 2: {naam indien toegewezen}

## Transcript

**10:32:15 - Speaker 1**
Goedemorgen allemaal, laten we beginnen met de standup.

**10:32:22 - Speaker 2**
Ja, ik heb gisteren de API afgerond en vandaag ga ik de tests schrijven.

...
```

## Foutafhandeling

### Te implementeren error scenarios

1. **Audio device niet beschikbaar**
   - Toon melding, refresh device lijst

2. **Deepgram connectie faalt**
   - Retry met exponential backoff (max 3 pogingen)
   - Blijf audio opnemen lokaal
   - Toon waarschuwing in UI

3. **Claude API faalt**
   - Toon foutmelding
   - Transcript blijft beschikbaar voor handmatige samenvatting

4. **Disk space laag**
   - Waarschuw gebruiker voor start opname

## Implementatie Volgorde

### Fase 1: Basis Setup
1. Solution en project structure aanmaken
2. NuGet packages installeren
3. DI container configureren
4. Basis MVVM setup met MainWindow

### Fase 2: Audio Capture
5. AudioDevice model en enumeration
6. IAudioCaptureService interface
7. Microphone capture implementatie
8. System audio (loopback) capture implementatie
9. Audio mixing
10. WAV file writing

### Fase 3: UI - Device Selection
11. DeviceSelectorView + ViewModel
12. Device ComboBoxes met binding
13. Audio level meters
14. Start/Stop/Pause buttons

### Fase 4: Transcriptie
15. ITranscriptionService interface
16. Deepgram WebSocket implementatie
17. Audio streaming naar Deepgram
18. Transcript ontvangen en parsen

### Fase 5: UI - Transcript
19. TranscriptView + ViewModel
20. Live transcript weergave
21. Auto-scroll functionaliteit
22. Speaker labels weergave

### Fase 6: Storage & Export
23. Session model en repository
24. LiteDB implementatie
25. Markdown export functie
26. Session opslaan bij stop

### Fase 7: Samenvatting
27. ISummaryService interface
28. Claude API implementatie
29. SummaryView + ViewModel
30. Samenvatting genereren en opslaan

### Fase 8: Polish
31. Error handling en logging
32. Settings scherm voor API keys
33. Session geschiedenis view
34. UI styling en polish

## Test Scenarios

1. **Happy path**: Start opname → transcriptie loopt → stop → export werkt
2. **Device wissel**: Verander device tijdens inactief → werkt correct
3. **Lange sessie**: 1+ uur opname zonder memory leaks
4. **Netwerk dropout**: Deepgram connectie valt weg → reconnect automatisch
5. **Geen API key**: Duidelijke foutmelding, audio opname werkt nog steeds

## Opmerkingen

- Deepgram free tier: $200 credit (~775 uur transcriptie)
- Test eerst met korte opnames (< 1 minuut)
- Speaker diarization werkt beter met duidelijk verschillende stemmen
- Systeemaudio capture pakt ALLE audio, inclusief notificatie geluiden
