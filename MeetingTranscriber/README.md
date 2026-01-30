# Meeting Transcriber

Een Windows-desktopapplicatie voor het real-time transcriberen van vergaderingen met behulp van Deepgram spraakherkenning en het genereren van samenvattingen met Claude AI.

## Functies

- **Real-time transcriptie**: Neemt audio op via microfoon en systeemgeluid en transcribeert dit live
- **Spreker herkenning**: Identificeert verschillende sprekers in de transcriptie (diarization)
- **Nederlandse taal**: Standaard geconfigureerd voor Nederlandse spraakherkenning
- **AI-samenvattingen**: Genereert automatische samenvattingen van vergaderingen met Claude
- **Sessie geschiedenis**: Bewaar en bekijk eerdere vergaderingen
- **Markdown export**: Exporteer transcripties en samenvattingen naar Markdown bestanden

## Vereisten

- Windows 10/11
- .NET 8.0 Runtime
- Deepgram API key (voor transcriptie)
- Claude/Anthropic API key (voor samenvattingen)

## Installatie

### Optie 1: Vanuit broncode

1. Clone de repository:
   ```bash
   git clone <repository-url>
   cd echolog/MeetingTranscriber
   ```

2. Bouw de applicatie:
   ```bash
   dotnet build src/MeetingTranscriber.App
   ```

3. Start de applicatie:
   ```bash
   dotnet run --project src/MeetingTranscriber.App
   ```

### Optie 2: Gepubliceerde versie

1. Ga naar de `publish` map
2. Gebruik de `win-x64` map voor een standaard installatie of `single-exe` voor een enkele executable
3. Start `MeetingTranscriber.exe`

## Eerste gebruik

Bij de eerste keer opstarten zal de applicatie aangeven dat er instellingen ontbreken. Volg deze stappen:

1. **Deepgram API key verkrijgen**:
   - Ga naar [https://console.deepgram.com](https://console.deepgram.com)
   - Maak een account aan of log in
   - Maak een nieuwe API key aan

2. **Claude API key verkrijgen**:
   - Ga naar [https://console.anthropic.com](https://console.anthropic.com)
   - Maak een account aan of log in
   - Maak een nieuwe API key aan

3. **Instellingen configureren**:
   - Open de applicatie
   - Klik op de instellingen knop (tandwiel icoon)
   - Vul de Deepgram en Claude API keys in
   - Klik op "Opslaan"

## Gebruik

### Een vergadering opnemen

1. Selecteer een microfoon in de apparaat selector
2. Optioneel: selecteer een systeemgeluid apparaat om ook computeraudio op te nemen
3. Klik op de **Start** knop om de opname te beginnen
4. De transcriptie verschijnt real-time in het hoofdvenster
5. Klik op **Pauze** om tijdelijk te stoppen
6. Klik op **Stop** om de opname te beëindigen en op te slaan

### Een samenvatting genereren

1. Na het stoppen van een opname, klik op **Samenvatting genereren**
2. De AI genereert een samenvatting van de vergadering
3. De samenvatting wordt automatisch opgeslagen bij de sessie

### Exporteren

1. Klik op **Exporteren** om de transcriptie en samenvatting als Markdown te bewaren
2. Bestanden worden opgeslagen in de sessie map

### Eerdere sessies bekijken

1. Klik op de **Geschiedenis** knop
2. Selecteer een sessie uit de lijst
3. Klik op **Laden** om de sessie te openen

## Configuratie

De instellingen worden opgeslagen in:
```
%APPDATA%\MeetingTranscriber\settings.json
```

### Beschikbare instellingen

| Instelling | Beschrijving | Standaard |
|------------|--------------|-----------|
| DeepgramApiKey | API key voor Deepgram | (vereist) |
| DeepgramLanguage | Taal voor transcriptie | nl |
| DeepgramModel | Deepgram model | nova-2 |
| ClaudeApiKey | API key voor Claude | (vereist) |
| ClaudeModel | Claude model | claude-sonnet-4-20250514 |
| AutoScroll | Automatisch scrollen | true |
| DarkTheme | Donker thema | true |

## Bestandslocaties

- **Instellingen**: `%APPDATA%\MeetingTranscriber\settings.json`
- **Sessies**: `%APPDATA%\MeetingTranscriber\sessions\`
- **Audio bestanden**: Per sessie in de sessie map

## Problemen oplossen

### De applicatie start niet

1. Controleer of .NET 8.0 Runtime is geïnstalleerd
2. Controleer de Windows Event Viewer voor foutmeldingen

### Transcriptie werkt niet

1. Controleer of de Deepgram API key correct is geconfigureerd
2. Controleer of er een actieve internetverbinding is
3. Controleer of de microfoon correct is geselecteerd

### Samenvatting werkt niet

1. Controleer of de Claude API key correct is geconfigureerd
2. Controleer of er voldoende credits zijn op het Anthropic account

## Technische details

- **Framework**: .NET 8.0 / WPF
- **Audio**: NAudio bibliotheek
- **Transcriptie**: Deepgram WebSocket API
- **AI**: Anthropic Claude API
- **Database**: LiteDB voor sessie opslag
- **UI**: Material Design themes

## Licentie

[Voeg hier de licentie-informatie toe]
