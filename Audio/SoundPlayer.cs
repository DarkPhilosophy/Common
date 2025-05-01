using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;
using System.Threading;
using Common.Logging;

namespace Common.Audio
{
    /// <summary>
    /// Provides sound playback functionality for applications.
    /// This class handles playing sound effects for UI interactions.
    /// </summary>
    public static class SoundPlayer
    {
        // Dictionary to store sound paths by name
        private static readonly Dictionary<string, string> SoundPaths = new Dictionary<string, string>();

        // Default sound names
        public const string ButtonClick = "ButtonClick";
        public const string Success = "Success";
        public const string Error = "Error";
        public const string Warning = "Warning";
        public const string Notification = "Notification";

        /// <summary>
        /// Registers a sound with a specific name and path.
        /// </summary>
        /// <param name="soundName">The name to associate with the sound.</param>
        /// <param name="soundPath">The path to the sound file.</param>
        public static void RegisterSound(string soundName, string soundPath)
        {
            SoundPaths[soundName] = soundPath;
        }

        /// <summary>
        /// Registers a sound with a specific name from embedded resources.
        /// </summary>
        /// <param name="soundName">The name to associate with the sound.</param>
        /// <param name="resourcePath">The resource path within the assembly.</param>
        /// <param name="assembly">The assembly containing the resource (null for entry assembly).</param>
#if NET6_0_OR_GREATER
        public static void RegisterEmbeddedSound(string soundName, string resourcePath, Assembly? assembly = null)
#else
        public static void RegisterEmbeddedSound(string soundName, string resourcePath, Assembly assembly = null)
#endif
        {
            // Store the resource path with a special prefix to indicate it's an embedded resource
#if NET6_0_OR_GREATER
            SoundPaths[soundName] = "embedded:" + resourcePath + "|" + (assembly?.FullName ?? Assembly.GetEntryAssembly()?.FullName ?? "");
#else
            SoundPaths[soundName] = "embedded:" + resourcePath + "|" + (assembly != null ? assembly.FullName : (Assembly.GetEntryAssembly() != null ? Assembly.GetEntryAssembly().FullName : ""));
#endif
        }

        /// <summary>
        /// Registers common sounds with default paths.
        /// </summary>
        /// <param name="basePath">The base path for sound files (default is assets/Sounds).</param>
        public static void RegisterCommonSounds(string basePath = "assets/Sounds")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("RegisterCommonSounds called");

                // Try different resource paths for the embedded sounds
                // The resource name format can vary depending on how the resources are embedded

                // Try with Common.Audio prefix (as specified in the LogicalName)
                RegisterEmbeddedSound(ButtonClick, "Common.Audio.ui-minimal-click.wav");

                // Try with CSVGenerator prefix
                RegisterEmbeddedSound(ButtonClick, "CSVGenerator.assets.Sounds.ui-minimal-click.wav");

                // Try with ConfigReplacer prefix
                RegisterEmbeddedSound(ButtonClick, "ConfigReplacer.assets.Sounds.ui-minimal-click.wav");

                // Try with just the filename
                RegisterEmbeddedSound(ButtonClick, "ui-minimal-click.wav");

                // Try with assets/Sounds prefix
                RegisterEmbeddedSound(ButtonClick, "assets.Sounds.ui-minimal-click.wav");

                // Also register the sounds as file paths as a fallback
                RegisterSound(ButtonClick, Path.Combine(basePath, "ui-minimal-click.wav"));
                RegisterSound(Success, Path.Combine(basePath, "success.wav"));
                RegisterSound(Error, Path.Combine(basePath, "error.wav"));
                RegisterSound(Warning, Path.Combine(basePath, "warning.wav"));
                RegisterSound(Notification, Path.Combine(basePath, "notification.wav"));

                // Try to list all embedded resources in the entry assembly to help with debugging
                try
                {
                    var assembly = Assembly.GetEntryAssembly();
                    if (assembly != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Available embedded resources in entry assembly:");
                        foreach (var resource in assembly.GetManifestResourceNames())
                        {
                            System.Diagnostics.Debug.WriteLine($"  - {resource}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error listing embedded resources: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception but continue
                System.Diagnostics.Debug.WriteLine($"Error registering common sounds: {ex.Message}");
            }
        }

        /// <summary>
        /// Plays a button click sound.
        /// </summary>
        public static void PlayButtonClickSound()
        {
            System.Diagnostics.Debug.WriteLine("PlayButtonClickSound called");
            try { Logger.Instance.LogInfo("PlayButtonClickSound called", true); } catch { }

            // Try to play the sound directly from the embedded resource
            try
            {
                // Try to find the assembly that contains the sound file
                var assemblies = new List<Assembly>();

                // Add assemblies with null checks
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null) assemblies.Add(entryAssembly);

                var executingAssembly = Assembly.GetExecutingAssembly();
                if (executingAssembly != null) assemblies.Add(executingAssembly);

                var callingAssembly = Assembly.GetCallingAssembly();
                if (callingAssembly != null) assemblies.Add(callingAssembly);

                // Try to load Common assembly explicitly
                try { assemblies.Add(Assembly.Load("Common")); } catch { }

                // Try to load CSVGenerator assembly explicitly
                try { assemblies.Add(Assembly.Load("CSVGenerator")); } catch { }

                // Try to load ConfigReplacer assembly explicitly
                try { assemblies.Add(Assembly.Load("ConfigReplacer")); } catch { }

                // Remove null assemblies
                assemblies.RemoveAll(a => a == null);

                // Try different resource paths
                var resourcePaths = new List<string>
                {
                    "Common.Audio.ui-minimal-click.wav",
                    "CSVGenerator.assets.Sounds.ui-minimal-click.wav",
                    "ConfigReplacer.assets.Sounds.ui-minimal-click.wav",
                    "ui-minimal-click.wav",
                    "assets.Sounds.ui-minimal-click.wav"
                };

                bool soundPlayed = false;

                foreach (var assembly in assemblies)
                {
                    if (assembly == null) continue;

                    // Log the assembly name
                    System.Diagnostics.Debug.WriteLine($"Checking assembly: {assembly.FullName}");
                    try { Logger.Instance.LogInfo($"Checking assembly: {assembly.FullName}", true); } catch { }

                    // Log all resources in the assembly
                    try
                    {
                        var resources = assembly.GetManifestResourceNames();
                        System.Diagnostics.Debug.WriteLine($"Resources in assembly {assembly.FullName}: {string.Join(", ", resources)}");
                        try { Logger.Instance.LogInfo($"Resources in assembly {assembly.FullName}: {string.Join(", ", resources)}", true); } catch { }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error getting resources from assembly {assembly.FullName}: {ex.Message}");
                        try { Logger.Instance.LogInfo($"Error getting resources from assembly {assembly.FullName}: {ex.Message}", true); } catch { }
                    }

                    foreach (var resourcePath in resourcePaths)
                    {
                        try
                        {
                            var stream = assembly.GetManifestResourceStream(resourcePath);
                            if (stream != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Found resource {resourcePath} in assembly {assembly.FullName}");
                                try { Logger.Instance.LogInfo($"Found resource {resourcePath} in assembly {assembly.FullName}", true); } catch { }

                                using (var player = new System.Media.SoundPlayer(stream))
                                {
                                    player.Play();
                                    soundPlayed = true;
                                    System.Diagnostics.Debug.WriteLine($"Successfully played sound from resource {resourcePath}");
                                    try { Logger.Instance.LogInfo($"Successfully played sound from resource {resourcePath}", true); } catch { }
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error accessing resource {resourcePath} in assembly {assembly.FullName}: {ex.Message}");
                            try { Logger.Instance.LogInfo($"Error accessing resource {resourcePath} in assembly {assembly.FullName}: {ex.Message}", true); } catch { }
                        }
                    }

                    if (soundPlayed) break;
                }

                if (!soundPlayed)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to play sound directly, falling back to PlayNamedSound");
                    try { Logger.Instance.LogInfo("Failed to play sound directly, falling back to PlayNamedSound", true); } catch { }
                    PlayNamedSound(ButtonClick);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in PlayButtonClickSound: {ex.Message}");
                try { Logger.Instance.LogInfo($"Error in PlayButtonClickSound: {ex.Message}", true); } catch { }

                // Fall back to the original method
                PlayNamedSound(ButtonClick);
            }
        }

        /// <summary>
        /// Plays a sound by its registered name.
        /// </summary>
        /// <param name="soundName">The name of the sound to play.</param>
        public static void PlayNamedSound(string soundName)
        {
            try
            {
                // Check if the sound is registered
#if NET6_0_OR_GREATER
                if (!SoundPaths.TryGetValue(soundName, out string? path) || path == null)
#else
                string path;
                if (!SoundPaths.TryGetValue(soundName, out path) || path == null)
#endif
                {
                    // If not registered, try to find it in the default location
                    path = Path.Combine("assets", "Sounds", soundName + ".wav");
                    System.Diagnostics.Debug.WriteLine($"Sound '{soundName}' not registered, trying default path: {path}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Playing registered sound '{soundName}' from path: {path}");
                }

                // Check if it's an embedded resource
                if (path.StartsWith("embedded:"))
                {
                    System.Diagnostics.Debug.WriteLine($"Playing embedded sound: {path}");
                    PlayEmbeddedSound(path.Substring(9));
                    return;
                }

                // Otherwise play from file path
                System.Diagnostics.Debug.WriteLine($"Playing sound from file: {path}");
                PlaySound(path);
            }
            catch (Exception ex)
            {
                // Log the exception but continue
                System.Diagnostics.Debug.WriteLine($"Error playing sound '{soundName}': {ex.Message}");
            }
        }

        /// <summary>
        /// Plays an embedded sound resource.
        /// </summary>
        /// <param name="resourceInfo">The resource information (path|assembly).</param>
        private static void PlayEmbeddedSound(string resourceInfo)
        {
            try
            {
                // Split the resource info into path and assembly
                string[] parts = resourceInfo.Split('|');
                string resourcePath = parts[0];
                string assemblyName = parts.Length > 1 ? parts[1] : "";

                // Log to both Debug and Logger
                System.Diagnostics.Debug.WriteLine($"Playing embedded sound: Resource path={resourcePath}, Assembly={assemblyName}");
                try { Logger.Instance.LogInfo($"Playing embedded sound: Resource path={resourcePath}, Assembly={assemblyName}", true); } catch { }

                // Try to find the assembly in multiple ways
#if NET6_0_OR_GREATER
                List<Assembly?> assemblies = new List<Assembly?>();
#else
                List<Assembly> assemblies = new List<Assembly>();
#endif

                // 1. Try the entry assembly
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    assemblies.Add(entryAssembly);
                }

                // 2. Try the executing assembly
                var executingAssembly = Assembly.GetExecutingAssembly();
                if (executingAssembly != null)
                {
                    assemblies.Add(executingAssembly);
                }

                // 3. Try the calling assembly
                var callingAssembly = Assembly.GetCallingAssembly();
                if (callingAssembly != null)
                {
                    assemblies.Add(callingAssembly);
                }

                // 4. Try to load by name if provided
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    try
                    {
                        assemblies.Add(Assembly.Load(assemblyName));
                    }
                    catch (Exception ex)
                    {
                        try { Logger.Instance.LogInfo($"Failed to load assembly {assemblyName}: {ex.Message}", true); } catch { }
                    }
                }

                // 5. Try to load Common assembly explicitly
                try
                {
                    assemblies.Add(Assembly.Load("Common"));
                }
                catch (Exception ex)
                {
                    try { Logger.Instance.LogInfo($"Failed to load Common assembly: {ex.Message}", true); } catch { }
                }

                // 6. Try to load CSVGenerator assembly explicitly
                try
                {
                    assemblies.Add(Assembly.Load("CSVGenerator"));
                }
                catch (Exception ex)
                {
                    try { Logger.Instance.LogInfo($"Failed to load CSVGenerator assembly: {ex.Message}", true); } catch { }
                }

                // 7. Try to load ConfigReplacer assembly explicitly
                try
                {
                    assemblies.Add(Assembly.Load("ConfigReplacer"));
                }
                catch (Exception ex)
                {
                    try { Logger.Instance.LogInfo($"Failed to load ConfigReplacer assembly: {ex.Message}", true); } catch { }
                }

                // Remove null assemblies
                assemblies.RemoveAll(a => a == null);

                // Log the assemblies we found (only in debug mode)
                try { Logger.Instance.LogInfo($"Found {assemblies.Count} assemblies to search for resources", false); } catch { }

                // Try different resource paths in each assembly
                List<string> resourcePaths = new List<string>
                {
                    resourcePath,
                    "Common.Audio.ui-minimal-click.wav",
                    "CSVGenerator.assets.Sounds.ui-minimal-click.wav",
                    "ConfigReplacer.assets.Sounds.ui-minimal-click.wav",
                    "ui-minimal-click.wav",
                    "assets.Sounds.ui-minimal-click.wav"
                };

                // We don't need to log all resources in all assemblies
                // This was causing excessive logging

                // Try to find and play the sound from any of the assemblies and resource paths
                bool soundPlayed = false;

                foreach (var asm in assemblies)
                {
                    if (asm == null) continue;

                    foreach (var resPath in resourcePaths)
                    {
                        try
                        {
                            // Try to get the resource stream
                            var stream = asm.GetManifestResourceStream(resPath);
                            if (stream != null)
                            {
                                // Log resource found with minimal information
                                try { Logger.Instance.LogInfo($"Found sound resource", false); } catch { }

                                try
                                {
                                    // Play the sound directly from the stream
#if NET6_0_OR_GREATER
                                    using (var player = new System.Media.SoundPlayer(stream))
                                    {
                                        player.Play();
                                    }
#else
                                    var player = new System.Media.SoundPlayer(stream);
                                    try
                                    {
                                        player.Play();
                                    }
                                    finally
                                    {
                                        player.Dispose();
                                    }
#endif
                                    soundPlayed = true;
                                    // Log success with minimal information
                                    try { Logger.Instance.LogInfo("Successfully played sound", false); } catch { }
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    try { Logger.Instance.LogInfo($"Failed to play sound directly from stream: {ex.Message}", false); } catch { }

                                    // If direct stream playback fails, try creating a temporary file
                                    try
                                    {
                                        // Create a temporary file to play the sound
                                        string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(resPath));
                                        using (var fileStream = File.Create(tempFile))
                                        {
                                            stream.Position = 0; // Reset stream position
                                            stream.CopyTo(fileStream);
                                        }

                                        // Play the temporary file
#if NET6_0_OR_GREATER
                                        using (var filePlayer = new System.Media.SoundPlayer(tempFile))
                                        {
                                            filePlayer.Play();
                                        }
#else
                                        var filePlayer = new System.Media.SoundPlayer(tempFile);
                                        try
                                        {
                                            filePlayer.Play();
                                        }
                                        finally
                                        {
                                            filePlayer.Dispose();
                                        }
#endif
                                        soundPlayed = true;
                                        try { Logger.Instance.LogInfo("Successfully played sound from temporary file", false); } catch { }
                                        break;
                                    }
                                    catch (Exception ex2)
                                    {
                                        try { Logger.Instance.LogInfo($"Failed to play sound from temporary file: {ex2.Message}", false); } catch { }
                                    }
                                }
                                finally
                                {
                                    stream.Dispose();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            try { Logger.Instance.LogInfo($"Error accessing sound resource: {ex.Message}", false); } catch { }
                        }
                    }

                    if (soundPlayed) break;
                }

                if (!soundPlayed)
                {
                    try { Logger.Instance.LogInfo("Failed to play sound from any resource", false); } catch { }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue
                try { Logger.Instance.LogInfo($"Error playing sound: {ex.Message}", false); } catch { }
            }
        }

        /// <summary>
        /// Plays a sound from the specified file path.
        /// </summary>
        /// <param name="soundPath">The path to the sound file.</param>
        public static void PlaySound(string soundPath)
        {
            try
            {
                if (File.Exists(soundPath))
                {
#if NET6_0_OR_GREATER
                    using (var player = new System.Media.SoundPlayer(soundPath))
                    {
                        player.Play();
                    }
#else
                    var player = new System.Media.SoundPlayer(soundPath);
                    try
                    {
                        player.Play();
                    }
                    finally
                    {
                        player.Dispose();
                    }
#endif
                }
                else
                {
                    // Try looking for the file in the application directory as fallback
                    string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, soundPath);
                    if (File.Exists(fallbackPath))
                    {
#if NET6_0_OR_GREATER
                        using (var player = new System.Media.SoundPlayer(fallbackPath))
                        {
                            player.Play();
                        }
#else
                        var player = new System.Media.SoundPlayer(fallbackPath);
                        try
                        {
                            player.Play();
                        }
                        finally
                        {
                            player.Dispose();
                        }
#endif
                    }
                }
            }
            catch
            {
                // Silently continue if sound playback fails
            }
        }
    }
}
