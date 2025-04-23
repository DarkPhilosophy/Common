using System;

namespace Common
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
        /// The configuration manager used to load and save configuration
        /// </summary>
        protected static ConfigManager ConfigManager { get; set; } = new ConfigManager();

        /// <summary>
        /// Initializes the configuration manager with the specified settings
        /// </summary>
        /// <param name="storageLocation">The storage location for configuration files</param>
        /// <param name="configFileName">The name of the configuration file</param>
        /// <param name="companyName">The company name used for application data folders</param>
        /// <param name="applicationName">The application name used for application data folders</param>
        /// <param name="customPath">The custom path for configuration files when using CustomPath storage location</param>
        public static void InitializeConfigManager(
            ConfigStorageLocation storageLocation = ConfigStorageLocation.ApplicationDirectory,
            string configFileName = "config.json",
            string companyName = "Flex",
            string applicationName = "Application",
            string customPath = "")
        {
            ConfigManager = new ConfigManager(
                storageLocation,
                configFileName,
                companyName,
                applicationName,
                customPath);
        }

        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        public static string GetConfigFilePath()
        {
            return ConfigManager.ConfigFilePath;
        }

        /// <summary>
        /// Saves the configuration to the configuration file
        /// </summary>
        public virtual void Save()
        {
            ConfigManager.Save(this);
        }
    }
}
