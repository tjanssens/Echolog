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

Bij de eerste keer opstarten zal de applicatie aangeven dat er instellingen ontbreken. Volg onderstaande handleidingen om de benodigde API keys te verkrijgen.

---

## API Keys verkrijgen

### Deepgram API Key (voor spraak-naar-tekst transcriptie)

Deepgram is een spraakherkenningsservice die audio omzet naar tekst. Je hebt een API key nodig om deze service te gebruiken.

**Stap 1: Account aanmaken**
1. Ga naar [https://console.deepgram.com/signup](https://console.deepgram.com/signup)
2. Vul je e-mailadres en wachtwoord in, of registreer via Google/GitHub
3. Bevestig je e-mailadres via de verificatiemail

**Stap 2: API Key aanmaken**
1. Log in op [https://console.deepgram.com](https://console.deepgram.com)
2. Klik in het linker menu op **"API Keys"**
3. Klik op de knop **"Create a New API Key"**
4. Geef de key een naam (bijv. "Meeting Transcriber")
5. Selecteer de gewenste rechten:
   - Voor deze applicatie is **"Member"** voldoende
6. Klik op **"Create Key"**
7. **Belangrijk**: Kopieer de API key direct en bewaar deze veilig. De key wordt slechts één keer getoond!

**Kosten en gratis tegoed**
- Nieuwe accounts krijgen $200 gratis tegoed
- Transcriptie kost ongeveer $0.0043 per minuut (Pay As You Go)
- Bekijk actuele prijzen op [https://deepgram.com/pricing](https://deepgram.com/pricing)

---

### Claude API Key (voor AI-samenvattingen)

Claude is een AI-assistent van Anthropic die de vergadertranscripties samenvat.

**Stap 1: Account aanmaken**
1. Ga naar [https://console.anthropic.com/signup](https://console.anthropic.com/signup)
2. Vul je e-mailadres in en klik op **"Continue with Email"**
3. Voer de verificatiecode in die je per e-mail ontvangt
4. Vul je naam en telefoonnummer in voor verificatie
5. Accepteer de gebruiksvoorwaarden

**Stap 2: Tegoed toevoegen**
1. Na het inloggen, ga naar **"Plans & Billing"** in het menu
2. Klik op **"Add Payment Method"** om een betaalmethode toe te voegen
3. Voeg tegoed toe aan je account (minimaal $5)

**Stap 3: API Key aanmaken**
1. Ga naar [https://console.anthropic.com/settings/keys](https://console.anthropic.com/settings/keys)
2. Klik op **"Create Key"**
3. Geef de key een naam (bijv. "Meeting Transcriber")
4. Klik op **"Create Key"**
5. **Belangrijk**: Kopieer de API key direct en bewaar deze veilig. De key wordt slechts één keer getoond!

**Kosten**
- Claude Sonnet: $3 per miljoen input tokens, $15 per miljoen output tokens
- Een gemiddelde samenvatting kost enkele centen
- Bekijk actuele prijzen op [https://www.anthropic.com/pricing](https://www.anthropic.com/pricing)

---

## API Keys configureren in de applicatie

1. Start de Meeting Transcriber applicatie
2. Bij de eerste keer opstarten verschijnt automatisch een melding over ontbrekende instellingen
3. Klik op **"Ja"** om naar de instellingen te gaan, of klik later op het **tandwiel icoon** (⚙️)
4. Vul de API keys in:
   - **Deepgram API Key**: Plak hier je Deepgram key
   - **Claude API Key**: Plak hier je Anthropic/Claude key
5. Klik op **"Opslaan"**
6. De applicatie is nu klaar voor gebruik

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
