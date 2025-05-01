# Biblioteca de Componente Comune (Common Components Library)

## Română

### Prezentare generală

Această bibliotecă conține componente standardizate care pot fi partajate între aplicațiile ConfigReplacer și CSVGenerator. Scopul este de a menține un comportament și aspect consistent între ambele aplicații, reducând duplicarea codului.

### Versiune curentă

**1.0.2.0** - [Vezi jurnalul de modificări](CHANGELOG.md)

### Componente

#### Logger

Clasa `Logger` oferă un sistem de jurnalizare standardizat cu formatare consistentă și indicatori emoji pentru diferite tipuri de mesaje.

##### Caracteristici:
- Model singleton pentru acces global
- Gestionare buffer pentru limitarea utilizării memoriei
- Sistem de notificare bazat pe evenimente
- Jurnalizare în consolă și UI
- Categorizare mesaje (eroare, avertisment, succes, info)
- Indicatori emoji pentru diferite tipuri de mesaje
- Jurnalizare structurată cu coduri de eroare

##### Utilizare:
```csharp
// Jurnalizează un mesaj obișnuit
Logger.Instance.LogMessage("Operație completată");

// Jurnalizează o eroare
Logger.Instance.LogError("Eșec la încărcarea fișierului");

// Jurnalizează un avertisment
Logger.Instance.LogWarning("Dimensiunea fișierului depășește limita recomandată");

// Jurnalizează un mesaj de succes
Logger.Instance.LogSuccess("Fișier salvat cu succes");

// Jurnalizează un mesaj informativ
Logger.Instance.LogInfo("Procesare începută");

// Jurnalizează doar în consolă (nu în UI)
Logger.Instance.LogMessage("Informații de depanare", consoleOnly: true);
```

#### ConfigManager

Clasa `ConfigManager` oferă gestionarea configurației cu suport pentru diferite locații de stocare.

##### Caracteristici:
- Suport pentru multiple locații de stocare (director aplicație, AppData local/roaming, cale personalizată)
- Gestionare automată a proprietăților lipsă
- Operațiuni thread-safe cu blocări
- Caching proprietăți pentru performanță
- Mecanisme de reîncercare pentru operațiuni de fișiere

##### Utilizare:
```csharp
// Inițializare cu numele aplicației
ConfigManager.Initialize("MyApplication");

// Obține o valoare de configurare
string value = ConfigManager.Instance.GetValue<string>("SettingName", "DefaultValue");

// Setează o valoare de configurare
ConfigManager.Instance.SetValue("SettingName", "NewValue");

// Salvează configurația
ConfigManager.Instance.Save();
```

#### AnimationManager

Clasa `AnimationManager` oferă animații standardizate pentru elementele UI, în special butoane.

##### Caracteristici:
- Model singleton pentru acces global
- Pensule înghețate pentru optimizarea performanței
- Efecte de strălucire cu intensitate personalizabilă
- Animații de pulsare a marginii
- Animații de corupție pentru butoane roșii
- Abordări de animație bazate pe șablon și conținut

##### Utilizare:
```csharp
// Aplică o strălucire albastră unui buton
AnimationManager.Instance.ApplyBlueButtonGlow(myButton);

// Aplică o strălucire roșie unui buton
AnimationManager.Instance.ApplyRedButtonGlow(myButton);

// Pornește o animație de pulsare a marginii
AnimationManager.Instance.StartPulsingBorderAnimation(myButton);

// Oprește o animație de pulsare a marginii
AnimationManager.Instance.StopPulsingBorderAnimation(myButton);

// Gestionează intrarea mouse-ului pe butonul roșu
AnimationManager.Instance.HandleRedButtonMouseEnter(myButton);

// Gestionează ieșirea mouse-ului de pe butonul roșu
AnimationManager.Instance.HandleRedButtonMouseLeave(myButton, UpdateButtonColors);

// Pornește o animație de corupție pentru butonul roșu
AnimationManager.Instance.StartRedButtonCorruptionAnimation(myButton);

// Oprește o animație de corupție pentru butonul roșu
AnimationManager.Instance.StopRedButtonCorruptionAnimation(myButton);

// Oprește toate animațiile
AnimationManager.Instance.StopAllAnimations();
```

#### LanguageManager

Clasa `LanguageManager` oferă funcționalitate de comutare a limbii pentru aplicații.

##### Caracteristici:
- Model singleton pentru acces global
- Gestionare dicționar de resurse
- Rotație limbă (Română <-> Engleză)
- Gestionare erori cu revenire la Română

##### Utilizare:
```csharp
// Inițializare cu numele aplicației
LanguageManager.Instance.Initialize("MyApplication");

// Comută la o limbă specifică
LanguageManager.Instance.SwitchLanguage("Română");

// Obține următoarea limbă în rotație
string nextLanguage = LanguageManager.Instance.GetNextLanguage(currentLanguage);

// Încarcă limba din configurație
LanguageManager.Instance.LoadLanguageFromConfig();
```

#### SoundPlayer

Clasa `SoundPlayer` oferă funcționalitate de redare a sunetelor pentru aplicații.

##### Caracteristici:
- Metode utilitare statice
- Multiple mecanisme de rezervă pentru redarea sunetelor
- Eșec silențios (nu va bloca aplicația dacă redarea sunetului eșuează)
- Jurnalizare detaliată pentru diagnosticarea problemelor

##### Utilizare:
```csharp
// Inițializare cu numele aplicației
SoundPlayer.Initialize("MyApplication");

// Redă un sunet de clic pe buton
SoundPlayer.PlayButtonClickSound();

// Redă un sunet personalizat
SoundPlayer.PlaySound("path/to/sound.wav");
```

#### AdManager

Clasa `AdManager` oferă funcționalitate de afișare a reclamelor text și imagine.

##### Caracteristici:
- Tranziții line între reclame
- Încărcare din rețea și local
- Formatare stil markdown pentru reclame text
- Rotație automată a reclamelor

##### Utilizare:
```csharp
// Inițializare cu numele aplicației
AdManager.Instance.Initialize("MyApplication");

// Încarcă reclame din rețea
AdManager.Instance.LoadAdsFromNetwork("\\\\server\\path\\to\\ads");

// Afișează următoarea reclamă
AdManager.Instance.ShowNextAd();

// Setează containerul de reclame
AdManager.Instance.SetAdContainer(adPanel);
```

### Arhitectură

Biblioteca este structurată pe mai multe componente:

```
Common/
├── Configuration/           # Gestionare configurație
│   ├── ConfigManager.cs     # Manager principal de configurație
│   └── ConfigModel.cs       # Model de date pentru configurație
├── Logging/                 # Sistem de jurnalizare
│   ├── Logger.cs            # Implementare logger
│   └── LogMessage.cs        # Model mesaj de jurnal
├── UI/                      # Componente UI
│   ├── Animation/           # Animații UI
│   │   └── AnimationManager.cs # Manager animații
│   ├── Language/            # Suport multilingv
│   │   └── LanguageManager.cs # Manager limbă
│   └── Ads/                 # Sistem reclame
│       └── AdManager.cs     # Manager reclame
├── Audio/                   # Componente audio
│   └── SoundPlayer.cs       # Player sunete
└── Utilities/               # Utilități generale
    ├── FileUtils.cs         # Utilități fișiere
    └── StringUtils.cs       # Utilități string
```

### Construirea din sursă

Biblioteca suportă multiple framework-uri țintă (.NET Framework 4.8, .NET 5.0-10.0). Utilizați comanda dotnet build cu parametrul `-f` pentru a specifica framework-ul țintă:

```powershell
dotnet build Common\Common.csproj -f net48 -c Release
```

Parametrii:
- `-f net48`: Specifică framework-ul țintă (.NET Framework 4.8)
- `-c Release`: Specifică configurația (Release)

### Ghid de integrare

#### Pasul 1: Adăugare referințe

Adăugați referințe la biblioteca Common în proiectele ConfigReplacer și CSVGenerator.

#### Pasul 2: Inițializare componente

Inițializați componentele în codul de pornire al aplicației:

```csharp
// În App.xaml.cs sau cod similar de pornire
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Inițializare componente
    LanguageManager.Instance.Initialize("MyApplication");
    SoundPlayer.Initialize("MyApplication");

    // Configurare handler eveniment logger
    Logger.Instance.OnLogMessage += (message, isError, isWarning, isSuccess, isInfo, consoleOnly) =>
    {
        if (!consoleOnly && Application.Current?.MainWindow is MainWindow mainWindow)
        {
            mainWindow.UpdateLogDisplay(message);
        }
    };
}
```

#### Pasul 3: Actualizare MainWindow

Actualizați clasa MainWindow pentru a utiliza componentele comune:

```csharp
public partial class MainWindow : Window
{
    public void LogMessage(string message, bool isError = false, bool isWarning = false, bool isSuccess = false, bool isInfo = false, bool consoleOnly = false)
    {
        Logger.Instance.LogMessage(message, isError, isWarning, isSuccess, isInfo, consoleOnly);
    }

    public void UpdateLogDisplay(string formattedMessage)
    {
        // Adaugă la jurnal cu o linie nouă dacă nu este gol
        if (!string.IsNullOrEmpty(_txtLog.Text))
        {
            _txtLog.AppendText(Environment.NewLine);
        }

        _txtLog.AppendText(formattedMessage);

        // Derulează la sfârșit
        _txtLog.ScrollToEnd();
    }

    private void BtnLanguageSwitch_Click(object sender, RoutedEventArgs e)
    {
        SoundPlayer.PlayButtonClickSound();

        string nextLanguage = LanguageManager.Instance.GetNextLanguage(_config.Language);
        LanguageManager.Instance.SwitchLanguage(nextLanguage);

        _config.Language = nextLanguage;
        _config.Save();

        // Actualizare UI după cum este necesar
    }
}
```

### Bune practici

1. **Jurnalizare consistentă**: Utilizați clasa Logger pentru toată jurnalizarea pentru a menține o formatare consistentă.

2. **Performanță animații**: AnimationManager utilizează pensule înghețate pentru o performanță mai bună. Evitați crearea de noi pensule pentru culori comune.

3. **Gestionare erori**: Toate componentele includ gestionare robustă a erorilor pentru a preveni blocările. Întotdeauna înfășurați operațiunile UI în blocuri try-catch.

4. **Gestionare resurse**: Eliminați resursele în mod corespunzător, în special când lucrați cu fluxuri și fișiere temporare.

5. **Localizare**: Utilizați LanguageManager pentru tot textul afișat utilizatorilor pentru a asigura o localizare adecvată.

### Depanare

#### Sunetul nu se redă

1. Asigurați-vă că fișierele de sunet sunt incluse în proiect cu acțiunea de build corectă (Embedded Resource).
2. Verificați că numele aplicației este inițializat corect în SoundPlayer.Initialize().
3. Verificați că calea fișierului de sunet este corectă dacă utilizați PlaySound() cu o cale personalizată.

#### Animațiile nu funcționează

1. Verificați că butonul are un șablon cu un element "ButtonBorder" pentru animații bazate pe șablon.
2. Asigurați-vă că RenderTransform al butonului este un ScaleTransform pentru animații de scalare.
3. Verificați că butonul nu este deja animat de un alt proces.

#### Limba nu se schimbă

1. Asigurați-vă că fișierele XAML de limbă sunt în locația corectă (assets/Languages/).
2. Verificați că numele aplicației este inițializat corect în LanguageManager.Initialize().
3. Verificați că fișierele XAML de limbă au acțiunea de build corectă (Resource).

### Licență

Acest proiect este licențiat sub Licența MIT - consultați fișierul [LICENSE](../LICENSE) pentru detalii.

## English

### Overview

This library contains standardized components that can be shared between the ConfigReplacer and CSVGenerator applications. The goal is to maintain consistent behavior and appearance across both applications while reducing code duplication.

### Current Version

**1.0.2.0** - [See changelog](CHANGELOG.md)

### Components

#### Logger

The `Logger` class provides a standardized logging system with consistent formatting and emoji indicators for different message types.

##### Features:
- Singleton pattern for global access
- Buffer management to limit memory usage
- Event-based notification system
- Console and UI logging
- Message categorization (error, warning, success, info)
- Emoji indicators for different message types
- Structured logging with error codes

##### Usage:
```csharp
// Log a regular message
Logger.Instance.LogMessage("Operation completed");

// Log an error
Logger.Instance.LogError("Failed to load file");

// Log a warning
Logger.Instance.LogWarning("File size exceeds recommended limit");

// Log a success message
Logger.Instance.LogSuccess("File saved successfully");

// Log an info message
Logger.Instance.LogInfo("Processing started");

// Log only to console (not to UI)
Logger.Instance.LogMessage("Debug information", consoleOnly: true);
```

#### ConfigManager

The `ConfigManager` class provides configuration management with support for different storage locations.

##### Features:
- Support for multiple storage locations (application directory, local/roaming AppData, custom path)
- Automatic handling of missing properties
- Thread-safe operations with locks
- Property caching for performance
- Retry mechanisms for file operations

##### Usage:
```csharp
// Initialize with application name
ConfigManager.Initialize("MyApplication");

// Get a configuration value
string value = ConfigManager.Instance.GetValue<string>("SettingName", "DefaultValue");

// Set a configuration value
ConfigManager.Instance.SetValue("SettingName", "NewValue");

// Save the configuration
ConfigManager.Instance.Save();
```

#### AnimationManager

The `AnimationManager` class provides standardized animations for UI elements, particularly buttons.

##### Features:
- Singleton pattern for global access
- Frozen brushes for performance optimization
- Glow effects with customizable intensity
- Pulsing border animations
- Red button corruption animations
- Template-based and content-based animation approaches

##### Usage:
```csharp
// Apply a blue glow to a button
AnimationManager.Instance.ApplyBlueButtonGlow(myButton);

// Apply a red glow to a button
AnimationManager.Instance.ApplyRedButtonGlow(myButton);

// Start a pulsing border animation
AnimationManager.Instance.StartPulsingBorderAnimation(myButton);

// Stop a pulsing border animation
AnimationManager.Instance.StopPulsingBorderAnimation(myButton);

// Handle red button mouse enter
AnimationManager.Instance.HandleRedButtonMouseEnter(myButton);

// Handle red button mouse leave
AnimationManager.Instance.HandleRedButtonMouseLeave(myButton, UpdateButtonColors);

// Start a red button corruption animation
AnimationManager.Instance.StartRedButtonCorruptionAnimation(myButton);

// Stop a red button corruption animation
AnimationManager.Instance.StopRedButtonCorruptionAnimation(myButton);

// Stop all animations
AnimationManager.Instance.StopAllAnimations();
```

#### LanguageManager

The `LanguageManager` class provides language switching functionality for applications.

##### Features:
- Singleton pattern for global access
- Resource dictionary management
- Language rotation (Romanian <-> English)
- Error handling with fallback to English

##### Usage:
```csharp
// Initialize with application name
LanguageManager.Instance.Initialize("MyApplication");

// Switch to a specific language
LanguageManager.Instance.SwitchLanguage("English");

// Get the next language in rotation
string nextLanguage = LanguageManager.Instance.GetNextLanguage(currentLanguage);

// Load language from config
LanguageManager.Instance.LoadLanguageFromConfig();
```

#### SoundPlayer

The `SoundPlayer` class provides sound playback functionality for applications.

##### Features:
- Static utility methods
- Multiple fallback mechanisms for sound playback
- Silent failure (won't crash the application if sound playback fails)
- Detailed logging for troubleshooting issues

##### Usage:
```csharp
// Initialize with application name
SoundPlayer.Initialize("MyApplication");

// Play a button click sound
SoundPlayer.PlayButtonClickSound();

// Play a custom sound
SoundPlayer.PlaySound("path/to/sound.wav");
```

#### AdManager

The `AdManager` class provides text and image ad display functionality.

##### Features:
- Smooth transitions between ads
- Network and local loading
- Markdown-style formatting for text ads
- Automatic ad rotation

##### Usage:
```csharp
// Initialize with application name
AdManager.Instance.Initialize("MyApplication");

// Load ads from network
AdManager.Instance.LoadAdsFromNetwork("\\\\server\\path\\to\\ads");

// Display the next ad
AdManager.Instance.ShowNextAd();

// Set the ad container
AdManager.Instance.SetAdContainer(adPanel);
```

### Architecture

The library is structured into several components:

```
Common/
├── Configuration/           # Configuration management
│   ├── ConfigManager.cs     # Main configuration manager
│   └── ConfigModel.cs       # Configuration data model
├── Logging/                 # Logging system
│   ├── Logger.cs            # Logger implementation
│   └── LogMessage.cs        # Log message model
├── UI/                      # UI components
│   ├── Animation/           # UI animations
│   │   └── AnimationManager.cs # Animation manager
│   ├── Language/            # Multilingual support
│   │   └── LanguageManager.cs # Language manager
│   └── Ads/                 # Advertisement system
│       └── AdManager.cs     # Ad manager
├── Audio/                   # Audio components
│   └── SoundPlayer.cs       # Sound player
└── Utilities/               # General utilities
    ├── FileUtils.cs         # File utilities
    └── StringUtils.cs       # String utilities
```

### Building from Source

The library supports multiple target frameworks (.NET Framework 4.8, .NET 5.0-10.0). Use the dotnet build command with the `-f` parameter to specify the target framework:

```powershell
dotnet build Common\Common.csproj -f net48 -c Release
```

Parameters:
- `-f net48`: Specifies the target framework (.NET Framework 4.8)
- `-c Release`: Specifies the configuration (Release)

### Integration Guide

#### Step 1: Add References

Add references to the Common library in both ConfigReplacer and CSVGenerator projects.

#### Step 2: Initialize Components

Initialize the components in the application startup code:

```csharp
// In App.xaml.cs or similar startup code
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Initialize components
    LanguageManager.Instance.Initialize("MyApplication");
    SoundPlayer.Initialize("MyApplication");

    // Set up logger event handler
    Logger.Instance.OnLogMessage += (message, isError, isWarning, isSuccess, isInfo, consoleOnly) =>
    {
        if (!consoleOnly && Application.Current?.MainWindow is MainWindow mainWindow)
        {
            mainWindow.UpdateLogDisplay(message);
        }
    };
}
```

#### Step 3: Update MainWindow

Update the MainWindow class to use the common components:

```csharp
public partial class MainWindow : Window
{
    public void LogMessage(string message, bool isError = false, bool isWarning = false, bool isSuccess = false, bool isInfo = false, bool consoleOnly = false)
    {
        Logger.Instance.LogMessage(message, isError, isWarning, isSuccess, isInfo, consoleOnly);
    }

    public void UpdateLogDisplay(string formattedMessage)
    {
        // Add to log with a new line if not empty
        if (!string.IsNullOrEmpty(_txtLog.Text))
        {
            _txtLog.AppendText(Environment.NewLine);
        }

        _txtLog.AppendText(formattedMessage);

        // Scroll to the end
        _txtLog.ScrollToEnd();
    }

    private void BtnLanguageSwitch_Click(object sender, RoutedEventArgs e)
    {
        SoundPlayer.PlayButtonClickSound();

        string nextLanguage = LanguageManager.Instance.GetNextLanguage(_config.Language);
        LanguageManager.Instance.SwitchLanguage(nextLanguage);

        _config.Language = nextLanguage;
        _config.Save();

        // Update UI as needed
    }
}
```

### Best Practices

1. **Consistent Logging**: Use the Logger class for all logging to maintain consistent formatting.

2. **Animation Performance**: The AnimationManager uses frozen brushes for better performance. Avoid creating new brushes for common colors.

3. **Error Handling**: All components include robust error handling to prevent crashes. Always wrap UI operations in try-catch blocks.

4. **Resource Management**: Dispose of resources properly, especially when working with streams and temporary files.

5. **Localization**: Use the LanguageManager for all text displayed to users to ensure proper localization.

### Troubleshooting

#### Sound Not Playing

1. Ensure the sound files are included in the project with the correct build action (Embedded Resource).
2. Check that the application name is correctly initialized in SoundPlayer.Initialize().
3. Verify that the sound file path is correct if using PlaySound() with a custom path.

#### Animations Not Working

1. Check that the button has a template with a "ButtonBorder" element for template-based animations.
2. Ensure the button's RenderTransform is a ScaleTransform for scale animations.
3. Verify that the button is not already being animated by another process.

#### Language Not Switching

1. Ensure the language XAML files are in the correct location (assets/Languages/).
2. Check that the application name is correctly initialized in LanguageManager.Initialize().
3. Verify that the language XAML files have the correct build action (Resource).

### License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

## Author

Adalbert Alexandru Ungureanu - [adalbertalexandru.ungureanu@flex.com](mailto:adalbertalexandru.ungureanu@flex.com)
