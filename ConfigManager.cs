using System;
using System.Collections.Generic;
using System.IO;

#if NET48
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
using System.Text.Json.Nodes;
#endif

namespace Common
{
    /// <summary>
    /// Defines the storage location for configuration files
    /// </summary>
    public enum ConfigStorageLocation
    {
        /// <summary>
        /// Store in the application's directory (same as executable)
        /// </summary>
        ApplicationDirectory,

        /// <summary>
        /// Store in the user's local application data folder
        /// </summary>
        LocalApplicationData,

        /// <summary>
        /// Store in the user's roaming application data folder
        /// </summary>
        RoamingApplicationData,

        /// <summary>
        /// Store in a custom path
        /// </summary>
        CustomPath
    }

    /// <summary>
    /// Manages configuration settings for applications
    /// </summary>
    public class ConfigManager
    {
#if NET48
        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };
#else
        private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
#endif

        /// <summary>
        /// Gets or sets the storage location for configuration files
        /// </summary>
        public ConfigStorageLocation StorageLocation { get; set; } = ConfigStorageLocation.ApplicationDirectory;

        /// <summary>
        /// Gets or sets the custom path for configuration files when using CustomPath storage location
        /// </summary>
        public string CustomPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the configuration file
        /// </summary>
        public string ConfigFileName { get; set; } = "config.json";

        /// <summary>
        /// Gets or sets the company name used for application data folders
        /// </summary>
        public string CompanyName { get; set; } = "Flex";

        /// <summary>
        /// Gets or sets the application name used for application data folders
        /// </summary>
        public string ApplicationName { get; set; } = "Application";

        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        public string ConfigFilePath
        {
            get
            {
                switch (StorageLocation)
                {
                    case ConfigStorageLocation.ApplicationDirectory:
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

                    case ConfigStorageLocation.LocalApplicationData:
                        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        return Path.Combine(localAppData, CompanyName, ApplicationName, ConfigFileName);

                    case ConfigStorageLocation.RoamingApplicationData:
                        string roamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        return Path.Combine(roamingAppData, CompanyName, ApplicationName, ConfigFileName);

                    case ConfigStorageLocation.CustomPath:
                        return Path.Combine(CustomPath, ConfigFileName);

                    default:
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the ConfigManager class
        /// </summary>
        public ConfigManager()
        {
        }

        /// <summary>
        /// Creates a new instance of the ConfigManager class with the specified storage location
        /// </summary>
        /// <param name="storageLocation">The storage location for configuration files</param>
        /// <param name="configFileName">The name of the configuration file</param>
        /// <param name="companyName">The company name used for application data folders</param>
        /// <param name="applicationName">The application name used for application data folders</param>
        /// <param name="customPath">The custom path for configuration files when using CustomPath storage location</param>
        public ConfigManager(
            ConfigStorageLocation storageLocation,
            string configFileName = "config.json",
            string companyName = "Flex",
            string applicationName = "Application",
            string customPath = "")
        {
            StorageLocation = storageLocation;
            ConfigFileName = configFileName;
            CompanyName = companyName;
            ApplicationName = applicationName;
            CustomPath = customPath;
        }

        /// <summary>
        /// Loads configuration settings from the configuration file
        /// </summary>
        /// <typeparam name="T">The type of configuration settings to load</typeparam>
        /// <param name="defaultConfig">The default configuration settings to use if loading fails</param>
        /// <returns>The loaded configuration settings</returns>
        public T Load<T>(T defaultConfig) where T : class, new()
        {
            try
            {
                // Ensure the directory exists
                EnsureDirectoryExists();

                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);

#if NET48
                    // First, try to parse as a generic JSON object to check for missing properties
                    JObject jsonObj = JObject.Parse(json);

                    // Get the properties of the default config
                    var defaultProperties = GetProperties(defaultConfig);

                    // Check if any properties are missing
                    bool hasMissingProperties = false;
                    foreach (var prop in defaultProperties)
                    {
                        if (jsonObj[prop.Key] == null)
                        {
                            hasMissingProperties = true;
                            break;
                        }
                    }

                    // If any properties are missing, we need to add them
                    if (hasMissingProperties)
                    {
                        // Add missing properties
                        foreach (var prop in defaultProperties)
                        {
                            if (jsonObj[prop.Key] == null)
                            {
                                jsonObj[prop.Key] = JToken.FromObject(prop.Value);
                                Logger.Instance.LogInfo($"Added missing {prop.Key} property to {ConfigFileName}", true);
                            }
                        }

                        // Serialize back to JSON and save
                        json = jsonObj.ToString(Formatting.Indented);
                        File.WriteAllText(ConfigFilePath, json);

                        Logger.Instance.LogInfo($"Updated {ConfigFileName} with missing properties", true);
                    }

                    // Now deserialize the JSON (either original or updated) into our config type
                    var config = JsonConvert.DeserializeObject<T>(json, _serializerSettings);
#else
                    // First, try to parse as a generic JSON object to check for missing properties
                    JsonNode jsonNode = JsonNode.Parse(json);

                    // Get the properties of the default config
                    var defaultProperties = GetProperties(defaultConfig);

                    // Check if any properties are missing
                    bool hasMissingProperties = false;
                    foreach (var prop in defaultProperties)
                    {
                        if (jsonNode[prop.Key] == null)
                        {
                            hasMissingProperties = true;
                            break;
                        }
                    }

                    // If any properties are missing, we need to add them
                    if (hasMissingProperties)
                    {
                        // Add missing properties
                        foreach (var prop in defaultProperties)
                        {
                            if (jsonNode[prop.Key] == null)
                            {
                                jsonNode[prop.Key] = JsonSerializer.SerializeToNode(prop.Value);
                                Logger.Instance.LogInfo($"Added missing {prop.Key} property to {ConfigFileName}", true);
                            }
                        }

                        // Serialize back to JSON and save
                        json = jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(ConfigFilePath, json);

                        Logger.Instance.LogInfo($"Updated {ConfigFileName} with missing properties", true);
                    }

                    // Now deserialize the JSON (either original or updated) into our config type
                    var config = JsonSerializer.Deserialize<T>(json, _serializerOptions);
#endif

                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error loading config from {ConfigFilePath}: {ex.Message}", true);
            }

            // Return default config if loading fails
            return CreateDefaultConfig(defaultConfig);
        }

        /// <summary>
        /// Saves configuration settings to the configuration file
        /// </summary>
        /// <typeparam name="T">The type of configuration settings to save</typeparam>
        /// <param name="config">The configuration settings to save</param>
        public void Save<T>(T config) where T : class, new()
        {
            try
            {
                // Ensure the directory exists
                EnsureDirectoryExists();

                // Check if the config file exists
                if (File.Exists(ConfigFilePath))
                {
                    try
                    {
                        // Read the existing JSON file
                        string existingJson = File.ReadAllText(ConfigFilePath);

#if NET48
                        // Parse the existing JSON
                        JObject existingConfig = JObject.Parse(existingJson);

                        // Get the properties of the config to save
                        var configProperties = GetProperties(config);

                        // Update only the properties from this instance
                        foreach (var prop in configProperties)
                        {
                            existingConfig[prop.Key] = JToken.FromObject(prop.Value);
                        }

                        // Serialize back to JSON and save
                        string updatedJson = existingConfig.ToString(Formatting.Indented);
                        File.WriteAllText(ConfigFilePath, updatedJson);
                        Logger.Instance.LogInfo($"Updated {ConfigFileName} with new values", true);
                        return;
                    }
                    catch (Newtonsoft.Json.JsonException)
                    {
                        // If there's an error parsing the existing JSON, fall back to full replacement
                        Logger.Instance.LogWarning($"Error parsing existing {ConfigFileName}, creating new file", true);
                    }
#else
                        // Parse the existing JSON
                        JsonNode existingConfig = JsonNode.Parse(existingJson);

                        // Get the properties of the config to save
                        var configProperties = GetProperties(config);

                        // Update only the properties from this instance
                        foreach (var prop in configProperties)
                        {
                            existingConfig[prop.Key] = JsonSerializer.SerializeToNode(prop.Value);
                        }

                        // Serialize back to JSON and save
                        string updatedJson = existingConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(ConfigFilePath, updatedJson);
                        Logger.Instance.LogInfo($"Updated {ConfigFileName} with new values", true);
                        return;
                    }
                    catch (JsonException)
                    {
                        // If there's an error parsing the existing JSON, fall back to full replacement
                        Logger.Instance.LogWarning($"Error parsing existing {ConfigFileName}, creating new file", true);
                    }
#endif
                }

                // If the file doesn't exist or there was an error parsing it, create a new one
#if NET48
                string json = JsonConvert.SerializeObject(config, _serializerSettings);
#else
                string json = JsonSerializer.Serialize(config, _serializerOptions);
#endif
                File.WriteAllText(ConfigFilePath, json);
                Logger.Instance.LogInfo($"Created new {ConfigFileName} file", true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error saving config to {ConfigFilePath}: {ex.Message}", true);
            }
        }

        /// <summary>
        /// Creates a default configuration file
        /// </summary>
        /// <typeparam name="T">The type of configuration settings to create</typeparam>
        /// <param name="defaultConfig">The default configuration settings</param>
        /// <returns>The default configuration settings</returns>
        private T CreateDefaultConfig<T>(T defaultConfig) where T : class, new()
        {
            try
            {
                // Ensure the directory exists
                EnsureDirectoryExists();

                // Save the default config
#if NET48
                string json = JsonConvert.SerializeObject(defaultConfig, _serializerSettings);
#else
                string json = JsonSerializer.Serialize(defaultConfig, _serializerOptions);
#endif
                File.WriteAllText(ConfigFilePath, json);
                Logger.Instance.LogInfo($"Created default {ConfigFileName} file", true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error creating default config: {ex.Message}", true);
            }

            return defaultConfig;
        }

        /// <summary>
        /// Gets the properties of an object as a dictionary
        /// </summary>
        /// <typeparam name="T">The type of object</typeparam>
        /// <param name="obj">The object to get properties from</param>
        /// <returns>A dictionary of property names and values</returns>
        private Dictionary<string, object> GetProperties<T>(T obj) where T : class
        {
            var properties = new Dictionary<string, object>();
            var type = obj.GetType();

            foreach (var prop in type.GetProperties())
            {
                if (prop.CanRead)
                {
                    var value = prop.GetValue(obj);
                    if (value != null)
                    {
                        properties[prop.Name] = value;
                    }
                }
            }

            return properties;
        }

        /// <summary>
        /// Ensures that the directory for the configuration file exists
        /// </summary>
        private void EnsureDirectoryExists()
        {
#if NET6_0_OR_GREATER
            string? directory = Path.GetDirectoryName(ConfigFilePath);
#else
            string directory = Path.GetDirectoryName(ConfigFilePath);
#endif
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
