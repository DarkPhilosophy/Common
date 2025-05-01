using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using Common.Logging;

#if NET48
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#else
using System.Text.Json;
#endif

namespace Common.Configuration
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

        private static readonly object _fileLock = new object();
        private static readonly Dictionary<Type, PropertyInfo[]> _propertyCache = new Dictionary<Type, PropertyInfo[]>();

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
                        if (string.IsNullOrEmpty(CustomPath))
                            throw new InvalidOperationException("CustomPath must be set when StorageLocation is CustomPath.");
                        return Path.Combine(CustomPath, ConfigFileName);

                    default:
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
                }
            }
        }

        /// <summary>
        /// Creates a new instance of the ConfigManager class
        /// </summary>
        public ConfigManager() { }

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
            ConfigFileName = configFileName ?? throw new ArgumentNullException(nameof(configFileName));
            CompanyName = companyName ?? throw new ArgumentNullException(nameof(companyName));
            ApplicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
            CustomPath = customPath ?? string.Empty;
        }

        /// <summary>
        /// Loads configuration settings from the configuration file
        /// </summary>
        /// <typeparam name="T">The type of configuration settings to load</typeparam>
        /// <param name="defaultConfig">The default configuration settings to use if loading fails</param>
        /// <returns>The loaded configuration settings</returns>
        public T Load<T>(T defaultConfig) where T : class, new()
        {
            if (defaultConfig == null)
                throw new ArgumentNullException(nameof(defaultConfig));

            try
            {
                EnsureDirectoryExists();

                lock (_fileLock)
                {
                    if (File.Exists(ConfigFilePath))
                    {
                        Logger.Instance.LogInfo($"Loading configuration from {ConfigFilePath}", true);
                        string json = ReadFileWithRetry(ConfigFilePath);
                        Logger.Instance.LogInfo($"Configuration file content: {json}", true);
                        try
                        {
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
                                WriteFileWithRetry(ConfigFilePath, json);
                                Logger.Instance.LogInfo($"Updated {ConfigFileName} with missing properties", true);
                            }

                            // Now deserialize the JSON (either original or updated) into our config type
                            var config = JsonConvert.DeserializeObject<T>(json, _serializerSettings);
                            if (config != null)
                            {
                                // Log the deserialized config
                                Logger.Instance.LogInfo($"Deserialized config from {ConfigFilePath}", true);

                                // Return the deserialized config
                                return config;
                            }
#else
                            // First, try to parse as a generic JSON object to check for missing properties
                            using (JsonDocument jsonDoc = JsonDocument.Parse(json))
                            {
                                // Get the properties of the default config
                                var defaultProperties = GetProperties(defaultConfig);

                                // Check if any properties are missing
                                bool hasMissingProperties = false;
                                foreach (var prop in defaultProperties)
                                {
                                    if (!jsonDoc.RootElement.TryGetProperty(prop.Key, out _))
                                    {
                                        hasMissingProperties = true;
                                        break;
                                    }
                                }

                                // If any properties are missing, we need to add them
                                if (hasMissingProperties)
                                {
                                    // Create a new dictionary from the existing JSON
                                    var jsonDict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                                    if (jsonDict == null)
                                    {
                                        jsonDict = new Dictionary<string, object>();
                                    }

                                    // Add missing properties
                                    foreach (var prop in defaultProperties)
                                    {
                                        if (!jsonDict.ContainsKey(prop.Key))
                                        {
                                            jsonDict[prop.Key] = prop.Value;
                                            Logger.Instance.LogInfo($"Added missing {prop.Key} property to {ConfigFileName}", true);
                                        }
                                    }

                                    // Serialize back to JSON and save
                                    json = JsonSerializer.Serialize(jsonDict, new JsonSerializerOptions { WriteIndented = true });
                                    WriteFileWithRetry(ConfigFilePath, json);
                                    Logger.Instance.LogInfo($"Updated {ConfigFileName} with missing properties", true);
                                }
                            }

                            // Now deserialize the JSON (either original or updated) into our config type
                            var config = JsonSerializer.Deserialize<T>(json, _serializerOptions);
                            if (config != null)
                            {
                                // Log the deserialized config
                                Logger.Instance.LogInfo($"Deserialized config from {ConfigFilePath}", true);

                                // Return the deserialized config
                                return config;
                            }
#endif
                        }
                        catch (JsonException ex)
                        {
                            var data = new Dictionary<string, object>
                            {
                                { "FilePath", ConfigFilePath },
                                { "ExceptionType", ex.GetType().Name }
                            };
                            Logger.Instance.LogError($"Error parsing {ConfigFilePath}: {ex.Message}", true, "CFG-002", data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string errorCode = "CFG-001"; // Default to file not found

                if (ex is UnauthorizedAccessException || ex is SecurityException)
                {
                    errorCode = "CFG-003"; // Access denied
                }
                else if (ex is DirectoryNotFoundException)
                {
                    errorCode = "CFG-004"; // Directory creation failed
                }
                else if (ex is IOException)
                {
                    errorCode = "CFG-005"; // File read error
                }

                var data = new Dictionary<string, object>
                {
                    { "FilePath", ConfigFilePath },
                    { "ExceptionType", ex.GetType().Name }
                };

                Logger.Instance.LogException(ex, $"Error loading config from {ConfigFilePath}", errorCode, true);
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
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            try
            {
                EnsureDirectoryExists();

                lock (_fileLock)
                {
                    // Check if the config file exists
                    if (File.Exists(ConfigFilePath))
                    {
                        try
                        {
                            // Read the existing JSON file
                            string existingJson = ReadFileWithRetry(ConfigFilePath);

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
                            WriteFileWithRetry(ConfigFilePath, updatedJson);
                            Logger.Instance.LogInfo($"Updated {ConfigFileName} with new values", true);
                            return;
#else
                            // Parse the existing JSON into a dictionary
                            var existingConfig = JsonSerializer.Deserialize<Dictionary<string, object>>(existingJson);
                            if (existingConfig == null)
                            {
                                existingConfig = new Dictionary<string, object>();
                            }

                            // Get the properties of the config to save
                            var configProperties = GetProperties(config);

                            // Update only the properties from this instance
                            foreach (var prop in configProperties)
                            {
                                existingConfig[prop.Key] = prop.Value;
                            }

                            // Serialize back to JSON and save
                            string updatedJson = JsonSerializer.Serialize(existingConfig, new JsonSerializerOptions { WriteIndented = true });
                            WriteFileWithRetry(ConfigFilePath, updatedJson);
                            Logger.Instance.LogInfo($"Updated {ConfigFileName} with new values", true);
                            return;
#endif
                        }
#if NET48
                        catch (JsonException ex)
                        {
                            // If there's an error parsing the existing JSON, fall back to full replacement
                            var data = new Dictionary<string, object>
                            {
                                { "FilePath", ConfigFilePath },
                                { "ExceptionType", ex.GetType().Name },
                                { "Action", "Creating new file" }
                            };
                            Logger.Instance.LogWarning($"Error parsing existing {ConfigFileName}, creating new file", true, data);
                        }
#else
                        catch (JsonException ex)
                        {
                            // If there's an error parsing the existing JSON, fall back to full replacement
                            var data = new Dictionary<string, object>
                            {
                                { "FilePath", ConfigFilePath },
                                { "ExceptionType", ex.GetType().Name },
                                { "Action", "Creating new file" }
                            };
                            Logger.Instance.LogWarning($"Error parsing existing {ConfigFileName}, creating new file", true, data);
                        }
#endif
                    }

                    // If the file doesn't exist or there was an error parsing it, create a new one
#if NET48
                    string json = JsonConvert.SerializeObject(config, _serializerSettings);
#else
                    string json = JsonSerializer.Serialize(config, _serializerOptions);
#endif
                    WriteFileWithRetry(ConfigFilePath, json);
                    Logger.Instance.LogInfo($"Created new {ConfigFileName} file", true);
                }
            }
            catch (Exception ex)
            {
                string errorCode = "CFG-006"; // Default to file write error

                if (ex is UnauthorizedAccessException || ex is SecurityException)
                {
                    errorCode = "CFG-003"; // Access denied
                }
                else if (ex is DirectoryNotFoundException)
                {
                    errorCode = "CFG-004"; // Directory creation failed
                }

                Logger.Instance.LogException(ex, $"Error saving config to {ConfigFilePath}", errorCode, true);
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
                EnsureDirectoryExists();

                lock (_fileLock)
                {
#if NET48
                    string json = JsonConvert.SerializeObject(defaultConfig, _serializerSettings);
#else
                    string json = JsonSerializer.Serialize(defaultConfig, _serializerOptions);
#endif
                    WriteFileWithRetry(ConfigFilePath, json);
                    Logger.Instance.LogInfo($"Created default {ConfigFileName} file", true);
                }
            }
            catch (Exception ex)
            {
                string errorCode = "CFG-006"; // Default to file write error

                if (ex is UnauthorizedAccessException || ex is SecurityException)
                {
                    errorCode = "CFG-003"; // Access denied
                }
                else if (ex is DirectoryNotFoundException)
                {
                    errorCode = "CFG-004"; // Directory creation failed
                }

                Logger.Instance.LogException(ex, "Error creating default config", errorCode, true);
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

            if (!_propertyCache.TryGetValue(type, out var props))
            {
                props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.CanRead)
                            .ToArray();
                _propertyCache[type] = props;
            }

            foreach (var prop in props)
            {
                if (prop.CanRead)
                {
                    var value = prop.GetValue(obj);
                    // Include all properties, even if they're null
                    properties[prop.Name] = value ?? GetDefaultValue(prop.PropertyType);
                }
            }

            return properties;
        }

        /// <summary>
        /// Gets the default value for a type
        /// </summary>
        /// <param name="type">The type to get the default value for</param>
        /// <returns>The default value for the type</returns>
        private object GetDefaultValue(Type type)
        {
            try
            {
#if NET6_0_OR_GREATER
                // Create an instance of the type
                object? instance = Activator.CreateInstance(type);

                // Return the instance if it's not null
                if (instance != null)
                {
                    return instance;
                }

                // If instance is null, create a new object based on type
                if (type.IsValueType)
                {
                    // For value types, create a new instance
                    return Activator.CreateInstance(type);
                }
                else if (type == typeof(string))
                {
                    // For strings, return an empty string
                    return string.Empty;
                }
                else
                {
                    // For other reference types, create a new object
                    return new object();
                }
#else
                if (type.IsValueType)
                {
                    return Activator.CreateInstance(type);
                }

                // For reference types, try to create an instance
                object instance = Activator.CreateInstance(type);
                if (instance != null)
                {
                    return instance;
                }

                // If instance is null, return appropriate default values
                if (type == typeof(string))
                {
                    return string.Empty;
                }
                else
                {
                    return new object();
                }
#endif
            }
            catch
            {
                // Handle exceptions by returning appropriate default values
                if (type.IsValueType)
                {
                    // For value types, create a new instance
                    return Activator.CreateInstance(type);
                }
                else if (type == typeof(string))
                {
                    // For strings, return an empty string
                    return string.Empty;
                }
                else
                {
                    // For other reference types, create a new object
                    return new object();
                }
            }
        }

        /// <summary>
        /// Ensures that the directory for the configuration file exists
        /// </summary>
        private void EnsureDirectoryExists()
        {
            try
            {
#if NET6_0_OR_GREATER
                string? directory = Path.GetDirectoryName(ConfigFilePath);
#else
                string directory = Path.GetDirectoryName(ConfigFilePath);
#endif
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Logger.Instance.LogInfo($"Created directory: {directory}", true);
                }
            }
            catch (Exception ex)
            {
                string errorCode = "CFG-004"; // Directory creation failed

                if (ex is UnauthorizedAccessException || ex is SecurityException)
                {
                    errorCode = "CFG-003"; // Access denied
                }

                var data = new Dictionary<string, object>
                {
                    { "FilePath", ConfigFilePath },
                    { "ExceptionType", ex.GetType().Name }
                };

                Logger.Instance.LogException(ex, "Failed to create directory for configuration file", errorCode, true);
                throw; // Rethrow to be handled by the calling method
            }
        }

        /// <summary>
        /// Reads a file with retry logic
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <param name="maxRetries">The maximum number of retries</param>
        /// <param name="delayMs">The delay between retries in milliseconds</param>
        /// <returns>The contents of the file</returns>
        private string ReadFileWithRetry(string path, int maxRetries = 3, int delayMs = 100)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return File.ReadAllText(path);
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    var data = new Dictionary<string, object>
                    {
                        { "FilePath", path },
                        { "RetryCount", i + 1 },
                        { "MaxRetries", maxRetries },
                        { "ExceptionType", ex.GetType().Name }
                    };
                    Logger.Instance.LogWarning($"Retry {i + 1}/{maxRetries} reading {path}: {ex.Message}", true, data);
                    Thread.Sleep(delayMs);
                }
            }
            return File.ReadAllText(path); // Final attempt, let it throw if it fails
        }

        /// <summary>
        /// Writes a file with retry logic
        /// </summary>
        /// <param name="path">The path to the file</param>
        /// <param name="content">The content to write</param>
        /// <param name="maxRetries">The maximum number of retries</param>
        /// <param name="delayMs">The delay between retries in milliseconds</param>
        private void WriteFileWithRetry(string path, string content, int maxRetries = 3, int delayMs = 100)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    File.WriteAllText(path, content);
                    return;
                }
                catch (IOException ex) when (i < maxRetries - 1)
                {
                    var data = new Dictionary<string, object>
                    {
                        { "FilePath", path },
                        { "RetryCount", i + 1 },
                        { "MaxRetries", maxRetries },
                        { "ExceptionType", ex.GetType().Name }
                    };
                    Logger.Instance.LogWarning($"Retry {i + 1}/{maxRetries} writing {path}: {ex.Message}", true, data);
                    Thread.Sleep(delayMs);
                }
            }
            File.WriteAllText(path, content); // Final attempt, let it throw if it fails
        }
    }
}
