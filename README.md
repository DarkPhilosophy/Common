# Common Components Library

This library contains standardized components that can be shared between the ConfigReplacer and CSVGenerator applications. The goal is to maintain consistent behavior and appearance across both applications while reducing code duplication.

## Components

### Logger

The `Logger` class provides a standardized logging system with consistent formatting and emoji indicators for different message types.

#### Features:
- Singleton pattern for global access
- Buffer management to limit memory usage
- Event-based notification system
- Console and UI logging
- Message categorization (error, warning, success, info)
- Emoji indicators for different message types

#### Usage:
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

### AnimationManager

The `AnimationManager` class provides standardized animations for UI elements, particularly buttons.

#### Features:
- Singleton pattern for global access
- Frozen brushes for performance optimization
- Glow effects with customizable intensity
- Pulsing border animations
- Red button corruption animations
- Template-based and content-based animation approaches

#### Usage:
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

### LanguageManager

The `LanguageManager` class provides language switching functionality for applications.

#### Features:
- Singleton pattern for global access
- Resource dictionary management
- Language rotation (English <-> Romanian)
- Error handling with fallback to English

#### Usage:
```csharp
// Initialize with application name
LanguageManager.Instance.Initialize("MyApplication");

// Switch to a specific language
LanguageManager.Instance.SwitchLanguage("Romanian");

// Get the next language in rotation
string nextLanguage = LanguageManager.Instance.GetNextLanguage(currentLanguage);

// Load language from config
LanguageManager.Instance.LoadLanguageFromConfig();
```

### SoundPlayer

The `SoundPlayer` class provides sound playback functionality for applications.

#### Features:
- Static utility methods
- Multiple fallback mechanisms for sound playback
- Silent failure (won't crash the application if sound playback fails)

#### Usage:
```csharp
// Initialize with application name
SoundPlayer.Initialize("MyApplication");

// Play a button click sound
SoundPlayer.PlayButtonClickSound();

// Play a custom sound
SoundPlayer.PlaySound("path/to/sound.wav");
```

## Integration Guide

### Step 1: Add References

Add references to the Common library in both ConfigReplacer and CSVGenerator projects.

### Step 2: Initialize Components

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

### Step 3: Update MainWindow

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

## Best Practices

1. **Consistent Logging**: Use the Logger class for all logging to maintain consistent formatting.

2. **Animation Performance**: The AnimationManager uses frozen brushes for better performance. Avoid creating new brushes for common colors.

3. **Error Handling**: All components include robust error handling to prevent crashes. Always wrap UI operations in try-catch blocks.

4. **Resource Management**: Dispose of resources properly, especially when working with streams and temporary files.

5. **Localization**: Use the LanguageManager for all text displayed to users to ensure proper localization.

## Troubleshooting

### Sound Not Playing

1. Ensure the sound files are included in the project with the correct build action (Embedded Resource).
2. Check that the application name is correctly initialized in SoundPlayer.Initialize().
3. Verify that the sound file path is correct if using PlaySound() with a custom path.

### Animations Not Working

1. Check that the button has a template with a "ButtonBorder" element for template-based animations.
2. Ensure the button's RenderTransform is a ScaleTransform for scale animations.
3. Verify that the button is not already being animated by another process.

### Language Not Switching

1. Ensure the language XAML files are in the correct location (assets/Languages/).
2. Check that the application name is correctly initialized in LanguageManager.Initialize().
3. Verify that the language XAML files have the correct build action (Resource).
