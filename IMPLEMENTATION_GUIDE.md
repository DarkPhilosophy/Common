# Implementation Guide for Common Components

This guide provides step-by-step instructions for integrating the Common Components Library into the ConfigReplacer and CSVGenerator projects.

## Prerequisites

- Visual Studio 2022 or later
- .NET 6.0 or later
- ConfigReplacer and CSVGenerator projects

## Step 1: Add the Common Project

1. Create a new Class Library project named "Common"
2. Add the Common project files:
   - Logger.cs
   - AnimationManager.cs
   - LanguageManager.cs
   - SoundPlayer.cs

3. Add references to required assemblies:
   ```xml
   <ItemGroup>
     <Reference Include="PresentationCore" />
     <Reference Include="PresentationFramework" />
     <Reference Include="System" />
     <Reference Include="System.Core" />
     <Reference Include="System.Xaml" />
     <Reference Include="WindowsBase" />
   </ItemGroup>
   ```

## Step 2: Add References to the Common Project

1. In ConfigReplacer project:
   - Right-click on "Dependencies" in Solution Explorer
   - Select "Add Project Reference..."
   - Check the "Common" project and click OK

2. In CSVGenerator project:
   - Right-click on "Dependencies" in Solution Explorer
   - Select "Add Project Reference..."
   - Check the "Common" project and click OK

## Step 3: Update ConfigReplacer Project

### 3.1 Update MainWindow.xaml.cs

1. Add using directive:
   ```csharp
   using Common;
   ```

2. Replace the LogMessage method:
   ```csharp
   private void LogMessage(string message, bool isError = false, bool isWarning = false, bool isSuccess = false, bool isInfo = false)
   {
       // Get the log text box if not already cached
       if (_txtLog == null)
       {
           _txtLog = (TextBox)FindName("txtLog");
           if (_txtLog == null) return;
       }

       // Get the log scroll viewer if not already cached
       if (_logScrollViewer == null)
       {
           _logScrollViewer = (ScrollViewer)FindName("logScrollViewer");
       }

       // Log using the common Logger
       Logger.Instance.LogMessage(message, isError, isWarning, isSuccess, isInfo);
   }
   ```

3. Add a method to handle log messages:
   ```csharp
   private void InitializeLogger()
   {
       Logger.Instance.OnLogMessage += (formattedMessage, isError, isWarning, isSuccess, isInfo, consoleOnly) =>
       {
           if (consoleOnly) return;

           // Get the log text box if not already cached
           if (_txtLog == null)
           {
               _txtLog = (TextBox)FindName("txtLog");
               if (_txtLog == null) return;
           }

           // Add to log with a new line if not empty
           if (!string.IsNullOrEmpty(_txtLog.Text))
           {
               _txtLog.AppendText(Environment.NewLine);
           }

           _txtLog.AppendText(formattedMessage);

           // Scroll to the end
           _txtLog.ScrollToEnd();

           // Ensure the ScrollViewer scrolls to the end as well
           if (_logScrollViewer != null)
           {
               _logScrollViewer.ScrollToEnd();

               // Force UI update to ensure scrolling happens immediately
               _logScrollViewer.UpdateLayout();
           }
       };
   }
   ```

4. Call InitializeLogger in the constructor:
   ```csharp
   public MainWindow()
   {
       InitializeComponent();
       InitializeLogger();
       // Rest of constructor code...
   }
   ```

5. Replace AnimationManager calls:
   ```csharp
   // Replace
   AnimationManager.Instance.StartPulsingBorderAnimation(button, Resources);
   // With
   Common.AnimationManager.Instance.StartPulsingBorderAnimation(button, Resources);

   // Replace
   AnimationManager.Instance.StopPulsingBorderAnimation(button);
   // With
   Common.AnimationManager.Instance.StopPulsingBorderAnimation(button);

   // Replace
   AnimationManager.Instance.ApplyBlueButtonGlow(button);
   // With
   Common.AnimationManager.Instance.ApplyBlueButtonGlow(button);

   // Replace
   AnimationManager.Instance.ApplyRedButtonGlow(button);
   // With
   Common.AnimationManager.Instance.ApplyRedButtonGlow(button);

   // Replace
   AnimationManager.Instance.StartRedButtonCorruptionAnimation(button);
   // With
   Common.AnimationManager.Instance.StartRedButtonCorruptionAnimation(button);

   // Replace
   AnimationManager.Instance.StopRedButtonCorruptionAnimation(button);
   // With
   Common.AnimationManager.Instance.StopRedButtonCorruptionAnimation(button);
   ```

6. Replace LanguageManager calls:
   ```csharp
   // In btnSwitchLanguage_Click method
   // Replace
   string nextLanguage = LanguageManager.Instance.GetNextLanguage(_config.Language);
   LanguageManager.Instance.SwitchLanguage(nextLanguage);
   // With
   string nextLanguage = Common.LanguageManager.Instance.GetNextLanguage(_config.Language);
   Common.LanguageManager.Instance.SwitchLanguage(nextLanguage);
   ```

7. Replace SoundPlayer calls:
   ```csharp
   // Replace
   SoundPlayer.PlayButtonClickSound();
   // With
   Common.SoundPlayer.PlayButtonClickSound();
   ```

### 3.2 Update App.xaml.cs

1. Add using directive:
   ```csharp
   using Common;
   ```

2. Initialize common components in OnStartup:
   ```csharp
   protected override void OnStartup(StartupEventArgs e)
   {
       base.OnStartup(e);

       // Initialize common components
       LanguageManager.Instance.Initialize("ConfigReplacer");
       SoundPlayer.Initialize("ConfigReplacer");
   }
   ```

### 3.3 Update AppConfig.cs

1. Add using directive:
   ```csharp
   using Common;
   ```

2. Replace console logging with Logger:
   ```csharp
   // Replace
   Console.WriteLine($"Error loading config: {ex.Message}");
   // With
   Logger.Instance.LogError($"Error loading config: {ex.Message}");

   // Replace
   Console.WriteLine("Added missing ConfigFilePaths property to config.json");
   // With
   Logger.Instance.LogInfo("Added missing ConfigFilePaths property to config.json");

   // Replace
   Console.WriteLine("Updated config.json with missing properties");
   // With
   Logger.Instance.LogInfo("Updated config.json with missing properties");
   ```

## Step 4: Update CSVGenerator Project

### 4.1 Update MainWindow.xaml.cs

1. Add using directive:
   ```csharp
   using Common;
   ```

2. Replace the LogMessage method:
   ```csharp
   public void LogMessage(string message, bool consoleOnly = false)
   {
       // Log using the common Logger
       Logger.Instance.LogMessage(message, consoleOnly: consoleOnly);
   }
   ```

3. Add a method to handle log messages:
   ```csharp
   private void InitializeLogger()
   {
       Logger.Instance.OnLogMessage += (formattedMessage, isError, isWarning, isSuccess, isInfo, consoleOnly) =>
       {
           // Always write to console for debugging
           Console.WriteLine(formattedMessage);

           // Only update log buffer if not console-only
           if (!consoleOnly)
           {
               // Add to log buffer - append to end to maintain chronological order
               _logBuffer.Append(formattedMessage + Environment.NewLine);

               // Limit buffer size to 100 lines
               string bufferText = _logBuffer.ToString();
               string[] lines = bufferText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
               if (lines.Length > 100)
               {
                   _logBuffer.Clear();
                   // Keep the most recent 100 lines
                   _logBuffer.Append(string.Join(Environment.NewLine, lines.Skip(lines.Length - 100)) + Environment.NewLine);
               }

               // Update detached log window if it exists and is visible
               if (_logWindow != null && _logWindow.IsVisible && _logWindowTextBox != null)
               {
                   _logWindowTextBox.AppendText(formattedMessage + Environment.NewLine);
                   _logWindowTextBox.ScrollToEnd();
               }
           }
       };
   }
   ```

4. Call InitializeLogger in the constructor:
   ```csharp
   public MainWindow()
   {
       InitializeComponent();
       InitializeLogger();
       // Rest of constructor code...
   }
   ```

5. Replace AnimationManager calls:
   ```csharp
   // Replace
   AnimationManager.Instance.StartPulsingBorderAnimation(button);
   // With
   Common.AnimationManager.Instance.StartPulsingBorderAnimation(button);

   // Replace
   AnimationManager.Instance.StopPulsingBorderAnimation(button);
   // With
   Common.AnimationManager.Instance.StopPulsingBorderAnimation(button);
   ```

6. Replace LanguageManager calls:
   ```csharp
   // In BtnLanguageSwitch_Click method
   // Replace
   string nextLanguage = LanguageManager.Instance.GetNextLanguage(_config.Language);
   LanguageManager.Instance.SwitchLanguage(nextLanguage);
   // With
   string nextLanguage = Common.LanguageManager.Instance.GetNextLanguage(_config.Language);
   Common.LanguageManager.Instance.SwitchLanguage(nextLanguage);
   ```

7. Replace SoundPlayer calls:
   ```csharp
   // Replace
   SoundPlayer.PlayButtonClickSound();
   // With
   Common.SoundPlayer.PlayButtonClickSound();
   ```

### 4.2 Update App.xaml.cs

1. Add using directive:
   ```csharp
   using Common;
   ```

2. Initialize common components in OnStartup:
   ```csharp
   protected override void OnStartup(StartupEventArgs e)
   {
       base.OnStartup(e);

       // Initialize common components
       LanguageManager.Instance.Initialize("CSVGenerator");
       SoundPlayer.Initialize("CSVGenerator");
   }
   ```

### 4.3 Update AppConfig.cs

1. Add using directive:
   ```csharp
   using Common;
   ```

2. Replace console logging with Logger:
   ```csharp
   // Replace
   Console.WriteLine($"Error loading config: {ex.Message}");
   // With
   Logger.Instance.LogError($"Error loading config: {ex.Message}");

   // Replace
   LogConfigChange("Added missing ClientList property to config.json");
   // With
   Logger.Instance.LogInfo("Added missing ClientList property to config.json");

   // Replace
   LogConfigChange("Updated config.json with missing properties");
   // With
   Logger.Instance.LogInfo("Updated config.json with missing properties");
   ```

3. Remove the LogConfigChange method as it's no longer needed

## Step 5: Remove Duplicate Classes

1. Remove the following classes from ConfigReplacer:
   - AnimationManager.cs
   - LanguageManager.cs
   - SoundPlayer.cs

2. Remove the following classes from CSVGenerator:
   - AnimationManager.cs
   - LanguageManager.cs
   - SoundPlayer.cs

## Step 6: Build and Test

1. Build the solution
2. Test ConfigReplacer:
   - Verify that logging works correctly
   - Verify that animations work correctly
   - Verify that language switching works correctly
   - Verify that sound playback works correctly

3. Test CSVGenerator:
   - Verify that logging works correctly
   - Verify that animations work correctly
   - Verify that language switching works correctly
   - Verify that sound playback works correctly

## Troubleshooting

### Issue: Missing Resources

If you encounter errors related to missing resources (e.g., language files or sound files), ensure that:

1. The resource files are included in the project with the correct build action
2. The application name is correctly initialized in LanguageManager.Initialize() and SoundPlayer.Initialize()
3. The resource paths are correct

### Issue: Namespace Conflicts

If you encounter namespace conflicts, use fully qualified names:

```csharp
// Instead of
using Common;

// Use fully qualified names
Common.Logger.Instance.LogMessage("Message");
Common.AnimationManager.Instance.StartPulsingBorderAnimation(button);
Common.LanguageManager.Instance.SwitchLanguage("English");
Common.SoundPlayer.PlayButtonClickSound();
```

### Issue: Event Handler Memory Leaks

If you're subscribing to the Logger.OnLogMessage event, make sure to unsubscribe when the window is closed:

```csharp
protected override void OnClosed(EventArgs e)
{
    base.OnClosed(e);
    
    // Unsubscribe from events
    Logger.Instance.OnLogMessage -= YourEventHandler;
}
```

## Conclusion

By following this guide, you should have successfully integrated the Common Components Library into both the ConfigReplacer and CSVGenerator projects. This integration provides consistent behavior and appearance across both applications while reducing code duplication.
