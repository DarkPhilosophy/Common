# Technical Specification: Common Components Library

## 1. Overview

The Common Components Library provides a set of standardized components for use across the ConfigReplacer and CSVGenerator applications. These components handle common functionality such as logging, animations, language management, and sound playback.

## 2. Components

### 2.1 Logger

#### 2.1.1 Purpose
Provides a standardized logging system with consistent formatting and emoji indicators for different message types.

#### 2.1.2 Class Definition
```csharp
namespace Common
{
    public class Logger
    {
        // Singleton instance
        private static Logger _instance;
        
        // Buffer to store log messages
        private readonly StringBuilder _logBuffer = new StringBuilder();
        
        // Maximum number of lines to keep in the buffer
        private const int MaxBufferLines = 100;
        
        // Delegate for log message callback
        public delegate void LogMessageCallback(string message, bool isError, bool isWarning, bool isSuccess, bool isInfo, bool consoleOnly);
        
        // Event for log message
        public event LogMessageCallback OnLogMessage;
        
        // Singleton accessor
        public static Logger Instance { get; }
        
        // Core logging method
        public void LogMessage(string message, bool isError = false, bool isWarning = false, bool isSuccess = false, bool isInfo = false, bool consoleOnly = false);
        
        // Convenience methods
        public void LogError(string message, bool consoleOnly = false);
        public void LogWarning(string message, bool consoleOnly = false);
        public void LogSuccess(string message, bool consoleOnly = false);
        public void LogInfo(string message, bool consoleOnly = false);
        
        // Buffer management
        public string GetLogBuffer();
        public void ClearLogBuffer();
    }
}
```

#### 2.1.3 Message Format
```
[Emoji] [Timestamp] Message
```

Where Emoji is one of:
- ❌ for errors
- ⚠️ for warnings
- ✅ for success
- 🔍 for info
- ℹ️ for regular messages

#### 2.1.4 Buffer Management
- Maximum of 100 lines stored in buffer
- When buffer exceeds 100 lines, oldest lines are removed
- Buffer can be cleared manually with ClearLogBuffer()

#### 2.1.5 Event System
- OnLogMessage event fired for each log message
- Event provides formatted message and type flags
- Applications can subscribe to handle log messages in UI

### 2.2 AnimationManager

#### 2.2.1 Purpose
Provides standardized animations for UI elements, particularly buttons.

#### 2.2.2 Class Definition
```csharp
namespace Common
{
    public class AnimationManager
    {
        // Singleton instance
        private static AnimationManager _instance;
        
        // Dictionaries for tracking animations and button states
        private readonly Dictionary<Button, Storyboard> _activeAnimations;
        private readonly Dictionary<Button, SolidColorBrush> _originalButtonColors;
        
        // Standard colors
        public static readonly SolidColorBrush BlueColor;
        public static readonly SolidColorBrush RedColor;
        public static readonly SolidColorBrush GreenColor;
        public static readonly SolidColorBrush YellowColor;
        public static readonly SolidColorBrush GrayColor;
        
        // Singleton accessor
        public static AnimationManager Instance { get; }
        
        // Glow effects
        public void ApplyBlueButtonGlow(Button button);
        public void ApplyRedButtonGlow(Button button);
        public void ApplyButtonGlow(Button button, Color color, int intensity);
        
        // Pulsing animations
        public void StartPulsingBorderAnimation(Button button, ResourceDictionary resourceDictionary = null);
        public void StopPulsingBorderAnimation(Button button);
        
        // Red button handling
        public void HandleRedButtonMouseEnter(Button button);
        public void HandleRedButtonMouseLeave(Button button, Action updateCallback);
        public void StartRedButtonCorruptionAnimation(Button button);
        public void StopRedButtonCorruptionAnimation(Button button);
        
        // Animation management
        public void StopAllAnimations();
    }
}
```

#### 2.2.3 Animation Types

##### Glow Effects
- Blue glow: Strong blue glow with pulsing intensity
- Red glow: Red glow with pulsing intensity
- Custom glow: Customizable color and intensity

##### Pulsing Border
- Template-based: Uses button template with ScaleTransform
- Content-based: Wraps button content in Border with animations

##### Red Button Corruption
- Changes button background to gray
- Adds red glow effect
- Changes cursor to "not allowed"
- Applies pulsing animation to glow

#### 2.2.4 Performance Optimizations
- Frozen brushes for standard colors
- Reuse of storyboards
- Efficient animation targeting

### 2.3 LanguageManager

#### 2.3.1 Purpose
Provides language switching functionality for applications.

#### 2.3.2 Class Definition
```csharp
namespace Common
{
    public class LanguageManager
    {
        // Singleton instance
        private static LanguageManager _instance;
        
        // Current language resource dictionary
        private ResourceDictionary _currentLanguageResource;
        
        // Application name for resource paths
        private readonly string _applicationName;
        
        // Singleton accessor
        public static LanguageManager Instance { get; }
        
        // Initialization
        public void Initialize(string applicationName);
        
        // Language management
        public void SwitchLanguage(string language);
        public string GetNextLanguage(string currentLanguage);
        public void LoadLanguageFromConfig();
    }
}
```

#### 2.3.3 Resource Path Format
```
/{ApplicationName};component/assets/Languages/{Language}.xaml
```

#### 2.3.4 Supported Languages
- English
- Romanian

#### 2.3.5 Error Handling
- Falls back to English if language resource cannot be loaded
- Logs errors but doesn't crash the application

### 2.4 SoundPlayer

#### 2.4.1 Purpose
Provides sound playback functionality for applications.

#### 2.4.2 Class Definition
```csharp
namespace Common
{
    public static class SoundPlayer
    {
        // Application name for resource paths
        private static string _applicationName;
        
        // Initialization
        public static void Initialize(string applicationName);
        
        // Sound playback
        public static void PlayButtonClickSound();
        public static void PlaySound(string soundPath);
    }
}
```

#### 2.4.3 Sound Playback Approach
1. Try to play from embedded resource
2. If that fails, try to create and play from temporary file
3. If that fails, try to play from file in application directory
4. If all approaches fail, silently continue

#### 2.4.4 Resource Path Format
```
{ApplicationName}.Audio.ui-minimal-click.wav
```

## 3. Dependencies

### 3.1 .NET Framework
- .NET 6.0 or later

### 3.2 WPF Dependencies
- PresentationCore
- PresentationFramework
- System.Xaml
- WindowsBase

### 3.3 System Dependencies
- System
- System.Core
- System.IO

## 4. Integration Requirements

### 4.1 Project Structure
- Common project as Class Library
- References from ConfigReplacer and CSVGenerator to Common

### 4.2 Resource Requirements
- Language XAML files in assets/Languages/
- Sound files in assets/Sounds/ or embedded as resources

### 4.3 Initialization
- Initialize LanguageManager with application name
- Initialize SoundPlayer with application name
- Subscribe to Logger.OnLogMessage event

## 5. Performance Considerations

### 5.1 Memory Usage
- Logger buffer limited to 100 lines
- Frozen brushes for standard colors
- Shared resources between applications

### 5.2 CPU Usage
- Efficient animation system
- Optimized resource loading
- Minimal garbage collection pressure

## 6. Error Handling

### 6.1 Logger
- Logs errors but doesn't throw exceptions
- Provides error logging methods

### 6.2 AnimationManager
- Catches and logs exceptions
- Gracefully handles animation failures

### 6.3 LanguageManager
- Falls back to English if language resource cannot be loaded
- Logs errors but doesn't crash the application

### 6.4 SoundPlayer
- Multiple fallback mechanisms
- Silently continues if sound playback fails

## 7. Extensibility

### 7.1 Adding New Languages
1. Create new XAML resource dictionary
2. Place in assets/Languages/ folder
3. Update GetNextLanguage method if needed

### 7.2 Adding New Animations
1. Add new methods to AnimationManager
2. Ensure proper cleanup in StopAllAnimations

### 7.3 Adding New Sounds
1. Add new methods to SoundPlayer
2. Ensure proper resource embedding

## 8. Testing Strategy

### 8.1 Unit Testing
- Test each component in isolation
- Mock dependencies where necessary
- Verify behavior with different inputs

### 8.2 Integration Testing
- Test components together
- Verify event handling
- Test resource loading

### 8.3 UI Testing
- Verify animations visually
- Test language switching
- Verify sound playback

## 9. Deployment

### 9.1 Build Process
- Build Common project first
- Reference Common.dll in ConfigReplacer and CSVGenerator
- Include Common.dll in deployment package

### 9.2 Resource Handling
- Embed language resources in applications
- Embed sound resources in applications
- Fall back to file system if embedded resources not available

## 10. Maintenance

### 10.1 Documentation
- XML documentation comments for all public members
- README.md with usage examples
- Implementation guide for integration

### 10.2 Version Control
- Track changes in Common project
- Update version number when making changes
- Document breaking changes

### 10.3 Bug Fixes
- Fix bugs in Common project
- Test fixes in both applications
- Document fixes in release notes
