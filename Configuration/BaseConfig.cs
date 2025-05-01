using System;
using Common.Logging;

namespace Common.Configuration
{
    /// <summary>
    /// Base class for application configuration
    /// </summary>
    public class BaseConfig
    {
        /// <summary>
        /// Gets or sets the language for the application
        /// </summary>
        public string Language { get; set; } = "Romanian";

        /// <summary>
        /// The configuration manager used to load and save configuration from the local directory
        /// </summary>
        protected static ConfigManager LocalConfigManager { get; set; } = new ConfigManager();

        /// <summary>
        /// The configuration manager used to load and save configuration from the AppData directory
        /// </summary>
        protected static ConfigManager AppDataConfigManager { get; set; } = new ConfigManager(ConfigStorageLocation.LocalApplicationData, "settings.json", "Flex", "Application");

        /// <summary>
        /// Flag indicating whether a local configuration file exists
        /// </summary>
        protected static bool LocalConfigExists { get; set; } = false;

        /// <summary>
        /// The currently active configuration manager
        /// </summary>
        protected static ConfigManager ActiveConfigManager => LocalConfigExists ? LocalConfigManager : (AppDataConfigManager ?? LocalConfigManager);

        /// <summary>
        /// Initializes the configuration manager with the specified settings
        /// </summary>
        /// <param name="configFileName">The name of the configuration file</param>
        /// <param name="companyName">The company name used for application data folders</param>
        /// <param name="applicationName">The application name used for application data folders</param>
        public static void InitializeConfigManager(
            string configFileName = "settings.json",
            string companyName = "Flex",
            string applicationName = "Application")
        {
            // Initialize the local configuration manager
            LocalConfigManager = new ConfigManager(
                ConfigStorageLocation.ApplicationDirectory,
                configFileName,
                companyName,
                applicationName);

            // Check if the local configuration file exists
            LocalConfigExists = System.IO.File.Exists(LocalConfigManager.ConfigFilePath);
            Logger.Instance.LogInfo($"Local configuration file {(LocalConfigExists ? "exists" : "does not exist")}: {LocalConfigManager.ConfigFilePath}", true);

            // Initialize the AppData configuration manager
            AppDataConfigManager = new ConfigManager(
                ConfigStorageLocation.LocalApplicationData,
                configFileName,
                companyName,
                applicationName);

            Logger.Instance.LogInfo($"AppData configuration file path: {AppDataConfigManager.ConfigFilePath}", true);
        }

        /// <summary>
        /// Gets the full path to the active configuration file
        /// </summary>
        public static string GetConfigFilePath()
        {
            return ActiveConfigManager.ConfigFilePath;
        }

        /// <summary>
        /// Gets the full path to the local configuration file
        /// </summary>
        public static string GetLocalConfigFilePath()
        {
            return LocalConfigManager.ConfigFilePath;
        }

        /// <summary>
        /// Gets the full path to the AppData configuration file
        /// </summary>
        public static string GetAppDataConfigFilePath()
        {
            return AppDataConfigManager?.ConfigFilePath ?? string.Empty;
        }

        /// <summary>
        /// Saves the configuration to the active configuration file
        /// </summary>
        public virtual void Save()
        {
            ActiveConfigManager.Save(this);
        }
    }
}
