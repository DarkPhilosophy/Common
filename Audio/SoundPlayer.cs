using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;
using Common.Logging;
using System.Windows;

namespace Common.Audio
{
    /// <summary>
    /// Dynamically plays sound effects, auto-detecting embedded or local sources.
    /// </summary>
    public static class SoundPlayer
    {
        private static readonly Dictionary<string, (string Path, Assembly Assembly)> SoundRegistry = new Dictionary<string, (string, Assembly)>();

        /// <summary>
        /// Registers a sound with a path and optional assembly.
        /// </summary>
        /// <param name="soundName">Sound identifier.</param>
        /// <param name="soundPath">Path (resource name or file path).</param>
        /// <param name="assembly">Assembly for embedded sound (default: entry assembly).</param>
        public static void RegisterSound(string soundName, string soundPath, Assembly assembly = null)
        {
            Logger.Instance.LogInfo($"SoundPlayer: RegisterSound called with name: '{soundName}', path: '{soundPath}', assembly: {assembly?.FullName ?? "null (will use entry assembly)"}", true);

            if (string.IsNullOrEmpty(soundName) || string.IsNullOrEmpty(soundPath))
            {
                Logger.Instance.LogWarning("SoundPlayer: Sound name or path cannot be empty.", true);
                return;
            }

            assembly = assembly ?? Assembly.GetEntryAssembly();
            if (assembly == null)
            {
                Logger.Instance.LogWarning("SoundPlayer: No assembly provided and entry assembly is null.", true);
                return;
            }

            Logger.Instance.LogInfo($"SoundPlayer: Using assembly: {assembly.FullName}", true);

            // Get all resources in the assembly
            var resources = assembly.GetManifestResourceNames();

            SoundRegistry[soundName] = (soundPath, assembly);
            Logger.Instance.LogInfo($"SoundPlayer: Successfully registered sound '{soundName}' with path '{soundPath}'", true);
        }

        /// <summary>
        /// Plays a sound by name, auto-detecting embedded or local source.
        /// </summary>
        /// <param name="soundName">Sound identifier.</param>
        public static void PlaySound(string soundName)
        {
            if (string.IsNullOrEmpty(soundName))
            {
                Logger.Instance.LogWarning("Sound name cannot be empty.", true);
                return;
            }

            Logger.Instance.LogInfo($"SoundPlayer: Attempting to play sound '{soundName}'", true);

            // Get executing and entry assemblies for later use if needed
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            Assembly entryAssembly = Assembly.GetEntryAssembly();

            if (!SoundRegistry.TryGetValue(soundName, out var soundInfo))
            {
                Logger.Instance.LogWarning($"Sound '{soundName}' not registered.", true);
                return;
            }

            Logger.Instance.LogInfo($"SoundPlayer: Found sound '{soundName}' in registry, path: '{soundInfo.Path}', has assembly: {soundInfo.Assembly != null}", true);
            PlaySoundInternal(soundInfo.Path, soundInfo.Assembly);
        }

        private static void PlaySoundInternal(string path, Assembly assembly)
        {
            Logger.Instance.LogInfo($"SoundPlayer: PlaySoundInternal called with path: '{path}', assembly: {assembly?.FullName ?? "null"}", true);

            // Try embedded first with the provided assembly
            Logger.Instance.LogInfo($"SoundPlayer: Trying to play embedded sound from path: '{path}' with provided assembly", true);
            if (TryPlayEmbeddedSound(path, assembly))
            {
                Logger.Instance.LogInfo($"SoundPlayer: Successfully played embedded sound from path: '{path}' with provided assembly", true);
                return;
            }

            // If that fails, try with the entry assembly if it's different
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly != null && entryAssembly != assembly)
            {
                Logger.Instance.LogInfo($"SoundPlayer: Trying to play embedded sound from path: '{path}' with entry assembly: {entryAssembly.FullName}", true);
                if (TryPlayEmbeddedSound(path, entryAssembly))
                {
                    Logger.Instance.LogInfo($"SoundPlayer: Successfully played embedded sound from path: '{path}' with entry assembly", true);
                    return;
                }
            }

            // Also try with the executing assembly if it's different
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            if (executingAssembly != assembly && executingAssembly != entryAssembly)
            {
                Logger.Instance.LogInfo($"SoundPlayer: Trying to play embedded sound from path: '{path}' with executing assembly: {executingAssembly.FullName}", true);
                if (TryPlayEmbeddedSound(path, executingAssembly))
                {
                    Logger.Instance.LogInfo($"SoundPlayer: Successfully played embedded sound from path: '{path}' with executing assembly", true);
                    return;
                }
            }

            // Fall back to local
            Logger.Instance.LogInfo($"SoundPlayer: Falling back to local sound from path: '{path}'", true);
            bool localSuccess = TryPlayLocalSound(path);
            Logger.Instance.LogInfo($"SoundPlayer: Local sound playback result: {(localSuccess ? "Success" : "Failed")}", true);
        }

        private static bool TryPlayEmbeddedSound(string resourcePath, Assembly assembly)
        {
            try
            {
                if (assembly == null)
                {
                    Logger.Instance.LogWarning($"SoundPlayer: Assembly is null for resource path: '{resourcePath}'", true);
                    return false;
                }

                Logger.Instance.LogInfo($"SoundPlayer: Getting manifest resource stream for path: '{resourcePath}' from assembly: {assembly.FullName}", true);

                // Get all resources in the assembly
                var resources = assembly.GetManifestResourceNames();

                // Check for any .g.resources files
                foreach (var resource in resources)
                {
                    if (resource.EndsWith(".g.resources"))
                    {
                        try
                        {
                            // Try to extract the sound from the .g.resources file
                            using (var gResourceStream = assembly.GetManifestResourceStream(resource))
                            {
                                if (gResourceStream != null)
                                {
                                    Logger.Instance.LogInfo($"SoundPlayer: Found {resource} with length: {gResourceStream.Length} bytes", true);

                                    // Create a ResourceReader to read the .g.resources file
                                    using (var reader = new System.Resources.ResourceReader(gResourceStream))
                                    {
                                        // Enumerate through all resources in the .g.resources file
                                        IDictionaryEnumerator enumerator = reader.GetEnumerator();
                                        while (enumerator.MoveNext())
                                        {
                                            string resourceName = enumerator.Key?.ToString() ?? string.Empty;
                                            Logger.Instance.LogInfo($"SoundPlayer: Found resource in {resource}: '{resourceName}'", true);

                                            // Extract the sound file name from the resourcePath
                                            string soundFileName = Path.GetFileName(resourcePath);

                                            // Check if this is our sound file - use more flexible matching
                                            if (!string.IsNullOrEmpty(soundFileName) &&
                                                (resourceName.EndsWith(soundFileName, StringComparison.OrdinalIgnoreCase) ||
                                                resourceName.EndsWith(Path.ChangeExtension(soundFileName, ".baml"), StringComparison.OrdinalIgnoreCase) ||
                                                resourceName.IndexOf(soundFileName, StringComparison.OrdinalIgnoreCase) >= 0))
                                            {
                                                Logger.Instance.LogInfo($"SoundPlayer: Found sound resource: '{resourceName}'", true);

                                                // Get the resource value (should be a byte array or Stream)
                                                object resourceValue = enumerator.Value;
                                                if (resourceValue is byte[] soundBytes)
                                                {
                                                    Logger.Instance.LogInfo($"SoundPlayer: Resource is byte array with length: {soundBytes.Length} bytes", true);

                                                    // Create a memory stream from the byte array
                                                    using (var memoryStream = new MemoryStream(soundBytes))
                                                    {
                                                        // Play the sound from the memory stream
                                                        System.Media.SoundPlayer player = new System.Media.SoundPlayer(memoryStream);
                                                        try
                                                        {
                                                            Logger.Instance.LogInfo($"SoundPlayer: Playing sound from {resource}: '{resourceName}'", true);
                                                            player.Play();
                                                            Logger.Instance.LogInfo($"SoundPlayer: Successfully played sound from {resource}: '{resourceName}'", true);
                                                            return true;
                                                        }
                                                        finally
                                                        {
                                                            player.Dispose();
                                                        }
                                                    }
                                                }
                                                else if (resourceValue is Stream resourceValueStream)
                                                {
                                                    Logger.Instance.LogInfo($"SoundPlayer: Resource is Stream with length: {resourceValueStream.Length} bytes", true);

                                                    // Play the sound from the stream
                                                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(resourceValueStream);
                                                    try
                                                    {
                                                        Logger.Instance.LogInfo($"SoundPlayer: Playing sound from {resource}: '{resourceName}'", true);
                                                        player.Play();
                                                        Logger.Instance.LogInfo($"SoundPlayer: Successfully played sound from {resource}: '{resourceName}'", true);
                                                        return true;
                                                    }
                                                    finally
                                                    {
                                                        player.Dispose();
                                                    }
                                                }
                                                else
                                                {
                                                    Logger.Instance.LogWarning($"SoundPlayer: Resource is not a byte array or Stream, it's a {resourceValue?.GetType().Name ?? "null"}", true);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogWarning($"SoundPlayer: Error reading {resource} file: {ex.Message}", true);
                            Logger.Instance.LogWarning($"SoundPlayer: Exception details: {ex}", true);
                        }
                    }
                }

                // Also check for direct embedded resources with the exact name
                try
                {
                    // Try to find the resource with the exact name
                    var directResourcePath = resourcePath;
                    Logger.Instance.LogInfo($"SoundPlayer: Trying direct resource path: '{directResourcePath}'", true);

                    using (var directStream = assembly.GetManifestResourceStream(directResourcePath))
                    {
                        if (directStream != null)
                        {
                            Logger.Instance.LogInfo($"SoundPlayer: Found direct resource with length: {directStream.Length} bytes", true);

                            // Play the sound from the stream
                            System.Media.SoundPlayer player = new System.Media.SoundPlayer(directStream);
                            try
                            {
                                Logger.Instance.LogInfo($"SoundPlayer: Playing sound from direct resource: '{directResourcePath}'", true);
                                player.Play();
                                Logger.Instance.LogInfo($"SoundPlayer: Successfully played sound from direct resource: '{directResourcePath}'", true);
                                return true;
                            }
                            finally
                            {
                                player.Dispose();
                            }
                        }
                    }

                    // Try to find the sound file in any embedded resource by searching through all resources
                    foreach (var resourceName in resources)
                    {
                        // Extract the sound file name from the resourcePath
                        string soundFileName = Path.GetFileName(resourcePath);

                        // Skip if the resource doesn't contain the sound file name
                        if (resourceName.IndexOf(soundFileName, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        Logger.Instance.LogInfo($"SoundPlayer: Trying to load sound directly from resource: '{resourceName}'", true);

                        try
                        {
                            using (var directStream = assembly.GetManifestResourceStream(resourceName))
                            {
                                if (directStream != null)
                                {
                                    Logger.Instance.LogInfo($"SoundPlayer: Found resource with length: {directStream.Length} bytes", true);

                                    // Play the sound from the stream
                                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(directStream);
                                    try
                                    {
                                        Logger.Instance.LogInfo($"SoundPlayer: Playing sound from resource: '{resourceName}'", true);
                                        player.Play();
                                        Logger.Instance.LogInfo($"SoundPlayer: Successfully played sound from resource: '{resourceName}'", true);
                                        return true;
                                    }
                                    finally
                                    {
                                        player.Dispose();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogWarning($"SoundPlayer: Error playing from resource {resourceName}: {ex.Message}", true);
                        }
                    }

                    // Look for .g.resources files and try to find the sound file inside them
                    foreach (var resourceName in resources)
                    {
                        if (resourceName.EndsWith(".g.resources"))
                        {
                            try
                            {
                                using (var gResourceStream = assembly.GetManifestResourceStream(resourceName))
                                {
                                    if (gResourceStream != null)
                                    {
                                        Logger.Instance.LogInfo($"SoundPlayer: Examining .g.resources file: {resourceName}", true);

                                        using (var reader = new ResourceReader(gResourceStream))
                                        {
                                            IDictionaryEnumerator enumerator = reader.GetEnumerator();
                                            string soundFileName = Path.GetFileName(resourcePath);

                                            while (enumerator.MoveNext())
                                            {
                                                string entryName = enumerator.Key?.ToString() ?? string.Empty;
                                                Logger.Instance.LogInfo($"SoundPlayer: Found entry in .g.resources: '{entryName}'", true);

                                                if (entryName.IndexOf(soundFileName, StringComparison.OrdinalIgnoreCase) >= 0)
                                                {
                                                    Logger.Instance.LogInfo($"SoundPlayer: Found matching sound entry: '{entryName}'", true);

                                                    object resourceValue = enumerator.Value;
                                                    if (resourceValue is byte[] soundBytes)
                                                    {
                                                        using (var memoryStream = new MemoryStream(soundBytes))
                                                        {
                                                            System.Media.SoundPlayer player = new System.Media.SoundPlayer(memoryStream);
                                                            try
                                                            {
                                                                Logger.Instance.LogInfo($"SoundPlayer: Playing sound from .g.resources entry: '{entryName}'", true);
                                                                player.Play();
                                                                Logger.Instance.LogInfo($"SoundPlayer: Successfully played sound from .g.resources entry: '{entryName}'", true);
                                                                return true;
                                                            }
                                                            finally
                                                            {
                                                                player.Dispose();
                                                            }
                                                        }
                                                    }
                                                    else if (resourceValue is Stream resourceValueStream)
                                                    {
                                                        System.Media.SoundPlayer player = new System.Media.SoundPlayer(resourceValueStream);
                                                        try
                                                        {
                                                            Logger.Instance.LogInfo($"SoundPlayer: Playing sound from .g.resources entry: '{entryName}'", true);
                                                            player.Play();
                                                            Logger.Instance.LogInfo($"SoundPlayer: Successfully played sound from .g.resources entry: '{entryName}'", true);
                                                            return true;
                                                        }
                                                        finally
                                                        {
                                                            player.Dispose();
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.LogWarning($"SoundPlayer: Error examining .g.resources file {resourceName}: {ex.Message}", true);
                            }
                        }
                    }

                    // Try to find the sound file with the exact resource name (for custom logical names)
                    try
                    {
                        // Try with the exact resource name as specified in the project file
                        foreach (var resource in resources)
                        {
                            if (resource.IndexOf(resourcePath.Replace('\\', '.').Replace('/', '.'), StringComparison.OrdinalIgnoreCase) >= 0 ||
                                resource.EndsWith(".g.resources.app.sounds." + Path.GetFileName(resourcePath), StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.Instance.LogInfo($"SoundPlayer: Found resource with matching path in name: '{resource}'", true);
                                using (var stream = assembly.GetManifestResourceStream(resource))
                                {
                                    if (stream != null)
                                    {
                                        System.Media.SoundPlayer player = new System.Media.SoundPlayer(stream);
                                        try
                                        {
                                            player.Play();
                                            Logger.Instance.LogInfo($"SoundPlayer: Successfully played sound from resource: '{resource}'", true);
                                            return true;
                                        }
                                        finally
                                        {
                                            player.Dispose();
                                        }
                                    }
                                }
                            }
                        }

                        // Try with the specific logical name format used in ConfigReplacer
                        string assemblyName = assembly.GetName().Name;
                        string logicalName = $"{assemblyName}.g.resources.app.sounds.{Path.GetFileName(resourcePath)}";
                        Logger.Instance.LogInfo($"SoundPlayer: Trying with logical name: '{logicalName}'", true);

                        using (var stream = assembly.GetManifestResourceStream(logicalName))
                        {
                            if (stream != null)
                            {
                                System.Media.SoundPlayer player = new System.Media.SoundPlayer(stream);
                                try
                                {
                                    player.Play();
                                    Logger.Instance.LogInfo($"SoundPlayer: Successfully played sound with logical name: '{logicalName}'", true);
                                    return true;
                                }
                                finally
                                {
                                    player.Dispose();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogWarning($"SoundPlayer: Error trying custom resource names: {ex.Message}", true);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogWarning($"SoundPlayer: Error reading direct resource: {ex.Message}", true);
                }

                // Fall back to the original method if the above didn't work
                Stream resourceStream = assembly.GetManifestResourceStream(resourcePath);
                if (resourceStream == null)
                {
                    Logger.Instance.LogWarning($"SoundPlayer: Stream is null for resource path: '{resourcePath}'", true);
                    return false;
                }

                Logger.Instance.LogInfo($"SoundPlayer: Got stream for resource path: '{resourcePath}', length: {resourceStream.Length} bytes", true);

                try
                {
                    Logger.Instance.LogInfo($"SoundPlayer: Creating SoundPlayer for resource: '{resourcePath}'", true);
                    System.Media.SoundPlayer player = new System.Media.SoundPlayer(resourceStream);
                    try
                    {
                        Logger.Instance.LogInfo($"SoundPlayer: Playing embedded sound: '{resourcePath}'", true);
                        player.Play();
                        Logger.Instance.LogInfo($"SoundPlayer: Successfully played embedded sound: '{resourcePath}'", true);
                        return true;
                    }
                    finally
                    {
                        player.Dispose();
                    }
                }
                finally
                {
                    resourceStream.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogWarning($"SoundPlayer: Failed to play embedded sound {resourcePath}: {ex.Message}", true);
                Logger.Instance.LogWarning($"SoundPlayer: Exception details: {ex}", true);
                return false;
            }
        }

        private static bool TryPlayLocalSound(string soundPath)
        {
            try
            {
                Logger.Instance.LogInfo($"SoundPlayer: TryPlayLocalSound called with path: '{soundPath}'", true);

                // Check if this is a WPF resource URI
                if (soundPath.StartsWith("pack://"))
                {
                    try
                    {
                        Logger.Instance.LogInfo($"SoundPlayer: Detected WPF resource URI: '{soundPath}'", true);

                        // Create a URI from the path
                        Uri resourceUri = new Uri(soundPath);
                        Logger.Instance.LogInfo($"SoundPlayer: Created URI: '{resourceUri}'", true);

                        // Open a stream to the resource
                        var streamResourceInfo = System.Windows.Application.GetResourceStream(resourceUri);
                        if (streamResourceInfo != null && streamResourceInfo.Stream != null)
                        {
                            Logger.Instance.LogInfo($"SoundPlayer: Got stream for WPF resource: '{soundPath}', length: {streamResourceInfo.Stream.Length} bytes", true);

                            // Play the sound
                            using (Stream stream = streamResourceInfo.Stream)
                            {
                                System.Media.SoundPlayer wpfPlayer = new System.Media.SoundPlayer(stream);
                                try
                                {
                                    Logger.Instance.LogInfo($"SoundPlayer: Playing WPF resource sound: '{soundPath}'", true);
                                    wpfPlayer.Play();
                                    Logger.Instance.LogInfo($"SoundPlayer: Successfully played WPF resource sound: '{soundPath}'", true);
                                    return true;
                                }
                                finally
                                {
                                    wpfPlayer.Dispose();
                                }
                            }
                        }
                        else
                        {
                            Logger.Instance.LogWarning($"SoundPlayer: Failed to get stream for WPF resource: '{soundPath}'", true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogWarning($"SoundPlayer: Failed to play WPF resource sound {soundPath}: {ex.Message}", true);
                        Logger.Instance.LogWarning($"SoundPlayer: Exception details: {ex}", true);
                    }
                }

                // Fall back to regular file path
                string path = File.Exists(soundPath) ? soundPath : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, soundPath);
                Logger.Instance.LogInfo($"SoundPlayer: Resolved local path: '{path}'", true);

                if (!File.Exists(path))
                {
                    Logger.Instance.LogWarning($"SoundPlayer: Local file not found: '{path}'", true);
                    return false;
                }

                Logger.Instance.LogInfo($"SoundPlayer: Local file exists: '{path}'", true);
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(path);
                try
                {
                    Logger.Instance.LogInfo($"SoundPlayer: Playing local sound: '{path}'", true);
                    player.Play();
                    Logger.Instance.LogInfo($"SoundPlayer: Successfully played local sound: '{path}'", true);
                    return true;
                }
                finally
                {
                    player.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogWarning($"SoundPlayer: Failed to play local sound {soundPath}: {ex.Message}", true);
                Logger.Instance.LogWarning($"SoundPlayer: Exception details: {ex}", true);
                return false;
            }
        }
    }
}