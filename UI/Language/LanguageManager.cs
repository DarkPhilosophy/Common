using System;
using System.Windows;
using Common.Logging;

namespace Common.UI.Language
{
    /// <summary>
    /// Provides language management functionality for applications.
    /// This class handles language switching and resource loading.
    /// </summary>
    public class LanguageManager
    {
        // Singleton instance
#if NET6_0_OR_GREATER
        private static LanguageManager? _instance;
#else
        private static LanguageManager _instance;
#endif

        // Current language resource dictionary
#if NET6_0_OR_GREATER
        private ResourceDictionary? _currentLanguageResource;
#else
        private ResourceDictionary _currentLanguageResource;
#endif

        // Application name for resource paths
        private string _applicationName;

        // Custom language path
        private string _languagePath = "assets/Languages";

        /// <summary>
        /// Gets the singleton instance of the LanguageManager.
        /// </summary>
        public static LanguageManager Instance
        {
            get
            {
#if NET6_0_OR_GREATER
                return _instance ??= new LanguageManager();
#else
                if (_instance == null)
                {
                    _instance = new LanguageManager();
                }
                return _instance;
#endif
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private LanguageManager()
        {
            // Default application name
            _applicationName = "Unknown";

            // Don't initialize resources here - wait for Initialize() to be called
        }

        /// <summary>
        /// Initializes the LanguageManager with a specific application name.
        /// </summary>
        /// <param name="applicationName">The name of the application for resource paths.</param>
        /// <param name="languagePath">Optional custom path for language files (default: "assets/Languages").</param>
        public void Initialize(string applicationName, string languagePath = "assets/Languages")
        {
            // Remove current language resource if it exists
            if (_currentLanguageResource != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(_currentLanguageResource);
            }

            // Set the application name and language path
            _applicationName = applicationName;
            _languagePath = languagePath;

            // Initialize with default language (English)
            _currentLanguageResource = new ResourceDictionary
            {
                Source = new Uri($"/{_applicationName};component/{_languagePath}/English.xaml", UriKind.Relative)
            };

            // Add to application resources
            Application.Current.Resources.MergedDictionaries.Add(_currentLanguageResource);
        }

        /// <summary>
        /// Switches the application language.
        /// </summary>
        /// <param name="language">The language to switch to (e.g., "English", "Romanian").</param>
        /// <param name="customPath">Optional custom path for this specific language switch.</param>
        public void SwitchLanguage(string language, string customPath = null)
        {
            try
            {
                // Use the provided custom path or fall back to the default
                string path = customPath ?? _languagePath;

                // Remove current language resource
                if (_currentLanguageResource != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(_currentLanguageResource);
                }

                // Load new language resource
                _currentLanguageResource = new ResourceDictionary
                {
                    Source = new Uri($"/{_applicationName};component/{path}/{language}.xaml", UriKind.Relative)
                };

                // Add to application resources
                Application.Current.Resources.MergedDictionaries.Add(_currentLanguageResource);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error switching language: {ex.Message}");

                // Fall back to English if there's an error
                try
                {
                    _currentLanguageResource = new ResourceDictionary
                    {
                        Source = new Uri($"/{_applicationName};component/{_languagePath}/English.xaml", UriKind.Relative)
                    };
                    Application.Current.Resources.MergedDictionaries.Add(_currentLanguageResource);
                }
                catch
                {
                    // If even the fallback fails, log it but don't crash
                    Logger.Instance.LogError("Failed to load fallback language (English)");
                }
            }
        }

        /// <summary>
        /// Gets the next language in the rotation.
        /// </summary>
        /// <param name="currentLanguage">The current language.</param>
        /// <returns>The next language in the rotation.</returns>
        public string GetNextLanguage(string currentLanguage)
        {
            return currentLanguage == "English" ? "Romanian" : "English";
        }

        /// <summary>
        /// Loads the language from the application configuration.
        /// </summary>
        /// <param name="language">The language to load from config.</param>
        /// <param name="customPath">Optional custom path for this specific language switch.</param>
        public void LoadLanguageFromConfig(string language = "English", string customPath = null)
        {
            try
            {
                // Switch to the specified language
                SwitchLanguage(language, customPath);
                Logger.Instance.LogInfo($"Loaded language: {language}", true); // Set to console-only to avoid duplicate messages
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error loading language from config: {ex.Message}");
                try
                {
                    // Fall back to English
                    SwitchLanguage("English", customPath);
                }
                catch
                {
                    // If even the fallback fails, log it but don't crash
                    Logger.Instance.LogError("Failed to load fallback language (English)");
                }
            }
        }
    }
}
