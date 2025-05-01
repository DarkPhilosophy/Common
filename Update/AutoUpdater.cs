using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
#if !NET48
using System.Net.Http;
#endif
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Common.Logging;

namespace Common.Update
{
    /// <summary>
    /// Provides auto-update functionality for applications.
    /// </summary>
    public class AutoUpdater
    {
#if NET48
        private static readonly WebClient _webClient = new WebClient();
#else
        private static readonly HttpClient _httpClient = new HttpClient();
#endif
        private readonly Logger _logger = Logger.Instance;
        private readonly string _applicationName;
        private readonly Version _currentVersion;
        private readonly string _executablePath;
        private readonly List<IUpdateSource> _updateSources = new List<IUpdateSource>();

        /// <summary>
        /// Initializes a new instance of the AutoUpdater class.
        /// </summary>
        /// <param name="applicationName">The name of the application to update.</param>
        /// <param name="currentVersion">The current version of the application. If null, will use the executing assembly's version.</param>
        /// <param name="executablePath">The path to the executable to update. If null, will use the executing assembly's location.</param>
        /// <param name="consoleOnly">Whether to log initialization messages to console only.</param>
        public AutoUpdater(string applicationName, Version currentVersion = null, string executablePath = null, bool consoleOnly = false)
        {
            _applicationName = applicationName ?? throw new ArgumentNullException(nameof(applicationName));
            _currentVersion = currentVersion ?? Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0, 0);
            _executablePath = executablePath ?? Process.GetCurrentProcess().MainModule?.FileName ??
                              throw new ArgumentException("Could not determine executable path. Please provide it explicitly.");

            // Log initialization message as console-only if requested
            _logger.LogInfo($"AutoUpdater initialized for {_applicationName} v{_currentVersion} at {_executablePath}", consoleOnly);
        }

        /// <summary>
        /// Adds a GitHub repository as an update source.
        /// </summary>
        /// <param name="owner">The GitHub repository owner.</param>
        /// <param name="repo">The GitHub repository name.</param>
        /// <param name="consoleOnly">Whether to log initialization messages to console only.</param>
        /// <returns>The AutoUpdater instance for method chaining.</returns>
        public AutoUpdater AddGitHubSource(string owner, string repo, bool consoleOnly = false)
        {
            _updateSources.Add(new GitHubUpdateSource(owner, repo));
            // Log source addition as console-only if requested
            _logger.LogInfo($"Added GitHub update source: {owner}/{repo}", consoleOnly);
            return this;
        }

        /// <summary>
        /// Adds a custom update source.
        /// </summary>
        /// <param name="updateSource">The update source to add.</param>
        /// <returns>The AutoUpdater instance for method chaining.</returns>
        public AutoUpdater AddUpdateSource(IUpdateSource updateSource)
        {
            _updateSources.Add(updateSource ?? throw new ArgumentNullException(nameof(updateSource)));
            _logger.LogInfo($"Added custom update source: {updateSource.GetType().Name}");
            return this;
        }

        /// <summary>
        /// Checks if an update is available from any of the configured update sources.
        /// </summary>
        /// <returns>Update information if an update is available; otherwise, null.</returns>
        public async Task<UpdateInfo> CheckForUpdateAsync()
        {
            // We'll log the source-specific message in the source check

            if (_updateSources.Count == 0)
            {
                _logger.LogWarning("No update sources configured. Add at least one update source before checking for updates.");
                return null;
            }

            foreach (var source in _updateSources)
            {
                try
                {
                    var updateInfo = await source.CheckForUpdateAsync(_applicationName, _currentVersion);
                    if (updateInfo != null)
                    {
                        _logger.LogInfo($"Update available: v{updateInfo.Version} from {source.GetType().Name}");
                        return updateInfo;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error checking for updates from {source.GetType().Name}: {ex.Message}");
                }
            }

            // Use the application's language resources for this message
            if (Application.Current.Resources.Contains("NoUpdatesAvailable"))
            {
                _logger.LogInfo(Application.Current.Resources["NoUpdatesAvailable"].ToString());
            }
            else
            {
                _logger.LogInfo("No updates available.");
            }
            return null;
        }

        /// <summary>
        /// Downloads and installs an update.
        /// </summary>
        /// <param name="updateInfo">The update information.</param>
        /// <param name="silent">Whether to perform the update silently without user interaction.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo, bool silent = false)
        {
            if (updateInfo == null)
                throw new ArgumentNullException(nameof(updateInfo));

            _logger.LogInfo($"Downloading update v{updateInfo.Version} from {updateInfo.DownloadUrl}...");

            try
            {
                // Create a temporary directory for the update
                string tempDir = Path.Combine(Path.GetTempPath(), $"{_applicationName}_update_{Guid.NewGuid()}");
                Directory.CreateDirectory(tempDir);

                // Download the update
                string downloadPath = Path.Combine(tempDir, Path.GetFileName(updateInfo.DownloadUrl));
#if NET48
                // Use WebClient for .NET Framework 4.8 with progress reporting
                var webClient = new WebClient();

                // Add progress reporting
                webClient.DownloadProgressChanged += (sender, e) => {
                    _logger.LogInfo($"Downloading update: {e.ProgressPercentage}% complete");
                };

                await webClient.DownloadFileTaskAsync(new Uri(updateInfo.DownloadUrl), downloadPath);
#else
                // Use HttpClient for .NET 5.0 and newer with progress reporting
                using (var response = await _httpClient.GetAsync(updateInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        var bytesRead = 0;
                        var totalBytesRead = 0L;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (totalBytes > 0)
                            {
                                var progressPercentage = (int)((totalBytesRead * 100) / totalBytes);
                                _logger.LogInfo($"Downloading update: {progressPercentage}% complete");
                            }
                        }
                    }
                }
#endif

                _logger.LogInfo($"Update downloaded to {downloadPath}");

                // If it's a direct executable replacement
                if (downloadPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return await InstallExecutableUpdateAsync(downloadPath, silent);
                }
                // If it's a ZIP file
                else if (downloadPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    return await InstallZipUpdateAsync(downloadPath, silent);
                }
                else
                {
                    _logger.LogError($"Unsupported update file format: {downloadPath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading or installing update: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Opens the update URL in the default browser.
        /// </summary>
        /// <param name="updateInfo">The update information.</param>
        /// <returns>True if the browser was opened successfully; otherwise, false.</returns>
        public bool OpenUpdateUrl(UpdateInfo updateInfo)
        {
            if (updateInfo == null)
                throw new ArgumentNullException(nameof(updateInfo));

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = updateInfo.ReleaseUrl,
                    UseShellExecute = true
                });
                _logger.LogInfo($"Opened update URL: {updateInfo.ReleaseUrl}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error opening update URL: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks for updates and prompts the user to install if available.
        /// </summary>
        /// <param name="silent">Whether to perform the update silently without user interaction.</param>
        /// <returns>True if an update was found and installed; otherwise, false.</returns>
        public async Task<bool> CheckAndInstallUpdateAsync(bool silent = false)
        {
            var updateInfo = await CheckForUpdateAsync();
            if (updateInfo == null)
                return false;

            if (silent || PromptForUpdate(updateInfo))
            {
                return await DownloadAndInstallUpdateAsync(updateInfo, silent);
            }

            return false;
        }

        /// <summary>
        /// Prompts the user to install an update.
        /// </summary>
        /// <param name="updateInfo">The update information.</param>
        /// <returns>True if the user accepted the update; otherwise, false.</returns>
        private bool PromptForUpdate(UpdateInfo updateInfo)
        {
            // This is a simple console prompt. In a real application, you would use a proper UI dialog.
            _logger.LogInfo($"An update is available: {_applicationName} v{updateInfo.Version}");
            _logger.LogInfo($"Current version: v{_currentVersion}");
            _logger.LogInfo($"Release notes: {updateInfo.ReleaseNotes}");
            _logger.LogInfo("Do you want to install this update? (Y/N)");

            var key = Console.ReadKey(true);
            return key.Key == ConsoleKey.Y;
        }

        /// <summary>
        /// Installs an executable update.
        /// </summary>
        /// <param name="updatePath">The path to the update executable.</param>
        /// <param name="silent">Whether to perform the update silently without user interaction.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        private async Task<bool> InstallExecutableUpdateAsync(string updatePath, bool silent)
        {
            _logger.LogInfo($"Installing executable update from {updatePath}...");

            try
            {
                // Create a batch file to replace the executable after the application exits
                string batchFilePath = Path.Combine(Path.GetTempPath(), $"update_{Guid.NewGuid()}.bat");
                string batchContent = $@"@echo off
echo Waiting for application to exit...
timeout /t 2 /nobreak > nul
echo Updating {_applicationName}...
copy /Y ""{updatePath}"" ""{_executablePath}""
echo Update complete!
start """" ""{_executablePath}""
del ""{batchFilePath}""
";
                File.WriteAllText(batchFilePath, batchContent);

                // Start the batch file and exit the application
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {batchFilePath}",
                    UseShellExecute = true,
                    CreateNoWindow = silent
                });

                // Give the batch file a moment to start
                await Task.Delay(500);

                // Exit the application to allow the update to proceed
                Environment.Exit(0);

                return true; // This line will never be reached, but is needed for compilation
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error installing executable update: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Installs a ZIP update.
        /// </summary>
        /// <param name="zipPath">The path to the ZIP file.</param>
        /// <param name="silent">Whether to perform the update silently without user interaction.</param>
        /// <returns>True if the update was successful; otherwise, false.</returns>
        private async Task<bool> InstallZipUpdateAsync(string zipPath, bool silent)
        {
            _logger.LogInfo($"Installing ZIP update from {zipPath}...");

            try
            {
                // Extract the ZIP file to a temporary directory
                string extractDir = Path.Combine(Path.GetTempPath(), $"{_applicationName}_extract_{Guid.NewGuid()}");
                Directory.CreateDirectory(extractDir);

                // Use System.IO.Compression if available, otherwise use a batch file with PowerShell
                string batchFilePath = Path.Combine(Path.GetTempPath(), $"update_{Guid.NewGuid()}.bat");
                string batchContent = $@"@echo off
echo Waiting for application to exit...
timeout /t 2 /nobreak > nul
echo Extracting update...
powershell -command ""Expand-Archive -Path '{zipPath}' -DestinationPath '{extractDir}' -Force""
echo Updating {_applicationName}...
xcopy /E /Y ""{extractDir}\*.*"" ""{Path.GetDirectoryName(_executablePath)}""
echo Update complete!
start """" ""{_executablePath}""
del ""{batchFilePath}""
";
                File.WriteAllText(batchFilePath, batchContent);

                // Start the batch file and exit the application
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {batchFilePath}",
                    UseShellExecute = true,
                    CreateNoWindow = silent
                });

                // Give the batch file a moment to start
                await Task.Delay(500);

                // Exit the application to allow the update to proceed
                Environment.Exit(0);

                return true; // This line will never be reached, but is needed for compilation
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error installing ZIP update: {ex.Message}");
                return false;
            }
        }
    }
}
