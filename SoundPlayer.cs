using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;

namespace Common
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
            // Register common sounds with default file names
            RegisterSound(ButtonClick, Path.Combine(basePath, "ui-minimal-click.wav"));
            RegisterSound(Success, Path.Combine(basePath, "success.wav"));
            RegisterSound(Error, Path.Combine(basePath, "error.wav"));
            RegisterSound(Warning, Path.Combine(basePath, "warning.wav"));
            RegisterSound(Notification, Path.Combine(basePath, "notification.wav"));
        }

        /// <summary>
        /// Plays a button click sound.
        /// </summary>
        public static void PlayButtonClickSound()
        {
            PlayNamedSound(ButtonClick);
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
                }

                // Check if it's an embedded resource
                if (path.StartsWith("embedded:"))
                {
                    PlayEmbeddedSound(path.Substring(9));
                    return;
                }

                // Otherwise play from file path
                PlaySound(path);
            }
            catch (Exception)
            {
                // Silently continue if sound playback fails
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

                // Get the assembly
#if NET6_0_OR_GREATER
                Assembly? assembly = null;
#else
                Assembly assembly = null;
#endif
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    try
                    {
                        assembly = Assembly.Load(assemblyName);
                    }
                    catch
                    {
                        // If we can't load the specified assembly, fall back to entry assembly
                        assembly = Assembly.GetEntryAssembly();
                    }
                }
                else
                {
                    assembly = Assembly.GetEntryAssembly();
                }

                if (assembly == null)
                {
                    return;
                }

#if NET6_0_OR_GREATER
                // Get the resource stream with using statement for automatic disposal
                using (var stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream != null)
                    {
                        try
                        {
                            // Play the sound directly from the stream
                            using (var player = new System.Media.SoundPlayer(stream))
                            {
                                player.Play();
                            }
                        }
                        catch
                        {
                            // If direct stream playback fails, try creating a temporary file
                            try
                            {
                                // Create a temporary file to play the sound
                                string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(resourcePath));
                                using (var fileStream = File.Create(tempFile))
                                {
                                    stream.Position = 0; // Reset stream position
                                    stream.CopyTo(fileStream);
                                }

                                // Play the temporary file
                                using (var filePlayer = new System.Media.SoundPlayer(tempFile))
                                {
                                    filePlayer.Play();
                                }
                            }
                            catch
                            {
                                // Silently continue if temporary file approach fails
                            }
                        }
                    }
                }
#else
                // Get the resource stream
                var stream = assembly.GetManifestResourceStream(resourcePath);
                try
                {
                    if (stream != null)
                    {
                        try
                        {
                            // Play the sound directly from the stream
                            var player = new System.Media.SoundPlayer(stream);
                            try
                            {
                                player.Play();
                            }
                            finally
                            {
                                player.Dispose();
                            }
                        }
                        catch
                        {
                            // If direct stream playback fails, try creating a temporary file
                            try
                            {
                                // Create a temporary file to play the sound
                                string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(resourcePath));
                                using (var fileStream = File.Create(tempFile))
                                {
                                    stream.Position = 0; // Reset stream position
                                    stream.CopyTo(fileStream);
                                }

                                // Play the temporary file
                                var filePlayer = new System.Media.SoundPlayer(tempFile);
                                try
                                {
                                    filePlayer.Play();
                                }
                                finally
                                {
                                    filePlayer.Dispose();
                                }
                            }
                            catch
                            {
                                // Silently continue if temporary file approach fails
                            }
                        }
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
#endif
            }
            catch
            {
                // Silently continue if sound playback fails
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
