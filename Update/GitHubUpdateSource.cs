using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
#if !NET48
using System.Net.Http;
using System.Text.Json;
#endif
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Common.Logging;
#if NET48
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace Common.Update
{
    /// <summary>
    /// Provides update checking functionality from GitHub releases.
    /// </summary>
    public class GitHubUpdateSource : IUpdateSource
    {
#if NET48
        private static readonly WebClient _webClient = new WebClient();
#else
        private static readonly HttpClient _httpClient = new HttpClient();
#endif
        private readonly Logger _logger = Logger.Instance;
        private readonly string _owner;
        private readonly string _repo;

        /// <summary>
        /// Initializes a new instance of the GitHubUpdateSource class.
        /// </summary>
        /// <param name="owner">The GitHub repository owner.</param>
        /// <param name="repo">The GitHub repository name.</param>
        public GitHubUpdateSource(string owner, string repo)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));

#if NET48
            // Set up the GitHub API user agent for WebClient
            _webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
#else
            // Set up the GitHub API user agent for HttpClient
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
            }
#endif
        }

        /// <summary>
        /// Checks if an update is available from GitHub releases.
        /// </summary>
        /// <param name="applicationName">The name of the application to check for updates.</param>
        /// <param name="currentVersion">The current version of the application.</param>
        /// <returns>Update information if an update is available; otherwise, null.</returns>
        public async Task<UpdateInfo> CheckForUpdateAsync(string applicationName, Version currentVersion)
        {
            try
            {
                // Use the application's language resources for this message
                if (Application.Current.Resources.Contains("CheckingForUpdatesFrom"))
                {
                    string message = string.Format(
                        Application.Current.Resources["CheckingForUpdatesFrom"].ToString(),
                        applicationName,
                        currentVersion,
                        $"GitHub: {_owner}/{_repo}");
                    _logger.LogInfo(message);
                }
                else
                {
                    _logger.LogInfo($"Checking for updates for {applicationName} v{currentVersion} from GitHub: {_owner}/{_repo}");
                }

                // Get the latest release from GitHub
                // Use the GitHub API with a fallback to the HTML page if API fails
                string apiUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
                string json;

#if NET48
                try
                {
                    // Use WebClient for .NET Framework 4.8
                    json = await _webClient.DownloadStringTaskAsync(apiUrl);
                }
                catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Forbidden)
                {
                    // If we get a 403 Forbidden, try to get the latest release from the HTML page
                    _logger.LogInfo("GitHub API rate limit exceeded, trying alternative method...");

                    // Use the releases page instead
                    string releasesUrl = $"https://github.com/{_owner}/{_repo}/releases";
                    string html = await _webClient.DownloadStringTaskAsync(releasesUrl);

                    // Extract the latest version from the HTML
                    var versionMatch = Regex.Match(html, @"releases/tag/v?([0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?)");
                    if (versionMatch.Success)
                    {
                        string versionString = versionMatch.Groups[1].Value;
                        Version latestVersion = Version.Parse(versionString);

                        // Try to extract release notes from the HTML
                        string releaseNotes = "See release notes on GitHub";
                        try
                        {
                            // Look for the release notes in the markdown-body class
                            var releaseNotesMatch = Regex.Match(html, @"<div class=""markdown-body[^""]*"">(.*?)</div>\s*</div>\s*</div>", RegexOptions.Singleline);
                            if (releaseNotesMatch.Success)
                            {
                                // Extract the content and clean it up
                                string rawNotes = releaseNotesMatch.Groups[1].Value;

                                // Remove HTML tags
                                rawNotes = Regex.Replace(rawNotes, @"<[^>]+>", " ");

                                // Replace multiple spaces with a single space
                                rawNotes = Regex.Replace(rawNotes, @"\s+", " ");

                                // Trim and limit length
                                rawNotes = rawNotes.Trim();
                                if (rawNotes.Length > 500)
                                {
                                    rawNotes = rawNotes.Substring(0, 497) + "...";
                                }

                                if (!string.IsNullOrWhiteSpace(rawNotes))
                                {
                                    releaseNotes = rawNotes;
                                }
                            }
                        }
                        catch (Exception htmlEx)
                        {
                            _logger.LogInfo($"Failed to extract release notes from HTML: {htmlEx.Message}. Using default message.");
                        }

                        // If the latest version is newer, return a basic UpdateInfo
                        if (latestVersion > currentVersion)
                        {
                            _logger.LogInfo($"New version available: {latestVersion} (current: {currentVersion})");

                            return new UpdateInfo(
                                latestVersion,
                                $"https://github.com/{_owner}/{_repo}/releases/latest",
                                $"https://github.com/{_owner}/{_repo}/releases/latest",
                                releaseNotes,
                                false,
                                DateTime.Now
                            );
                        }
                        else
                        {
                            // Use the application's language resources for this message
                            if (Application.Current.Resources.Contains("NoNewVersionAvailable"))
                            {
                                string message = string.Format(
                                    Application.Current.Resources["NoNewVersionAvailable"].ToString(),
                                    latestVersion,
                                    currentVersion);
                                _logger.LogInfo(message);
                            }
                            else
                            {
                                _logger.LogInfo($"No new version available. Latest: {latestVersion}, Current: {currentVersion}");
                            }
                            return null;
                        }
                    }

                    // If we couldn't extract the version, rethrow the original exception
                    throw;
                }

                // Parse JSON using Newtonsoft.Json
                using (var reader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var root = JObject.Load(jsonReader);
#else
                try
                {
                    // Use HttpClient for .NET 5.0 and newer
                    var response = await _httpClient.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    json = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("403"))
                {
                    // If we get a 403 Forbidden, try to get the latest release from the HTML page
                    _logger.LogInfo("GitHub API rate limit exceeded, trying alternative method...");

                    // Use the releases page instead
                    string releasesUrl = $"https://github.com/{_owner}/{_repo}/releases";
                    var response = await _httpClient.GetAsync(releasesUrl);
                    response.EnsureSuccessStatusCode();

                    string html = await response.Content.ReadAsStringAsync();

                    // Extract the latest version from the HTML
                    var versionMatch = Regex.Match(html, @"releases/tag/v?([0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?)");
                    if (versionMatch.Success)
                    {
                        string versionString = versionMatch.Groups[1].Value;
                        Version latestVersion = Version.Parse(versionString);

                        // Try to extract release notes from the HTML
                        string releaseNotes = "See release notes on GitHub";
                        try
                        {
                            // Look for the release notes in the markdown-body class
                            var releaseNotesMatch = Regex.Match(html, @"<div class=""markdown-body[^""]*"">(.*?)</div>\s*</div>\s*</div>", RegexOptions.Singleline);
                            if (releaseNotesMatch.Success)
                            {
                                // Extract the content and clean it up
                                string rawNotes = releaseNotesMatch.Groups[1].Value;

                                // Remove HTML tags
                                rawNotes = Regex.Replace(rawNotes, @"<[^>]+>", " ");

                                // Replace multiple spaces with a single space
                                rawNotes = Regex.Replace(rawNotes, @"\s+", " ");

                                // Trim and limit length
                                rawNotes = rawNotes.Trim();
                                if (rawNotes.Length > 500)
                                {
                                    rawNotes = rawNotes.Substring(0, 497) + "...";
                                }

                                if (!string.IsNullOrWhiteSpace(rawNotes))
                                {
                                    releaseNotes = rawNotes;
                                }
                            }
                        }
                        catch (Exception htmlEx)
                        {
                            _logger.LogInfo($"Failed to extract release notes from HTML: {htmlEx.Message}. Using default message.");
                        }

                        // If the latest version is newer, return a basic UpdateInfo
                        if (latestVersion > currentVersion)
                        {
                            _logger.LogInfo($"New version available: {latestVersion} (current: {currentVersion})");

                            return new UpdateInfo(
                                latestVersion,
                                $"https://github.com/{_owner}/{_repo}/releases/latest",
                                $"https://github.com/{_owner}/{_repo}/releases/latest",
                                releaseNotes,
                                false,
                                DateTime.Now
                            );
                        }
                        else
                        {
                            // Use the application's language resources for this message
                            if (Application.Current.Resources.Contains("NoNewVersionAvailable"))
                            {
                                string message = string.Format(
                                    Application.Current.Resources["NoNewVersionAvailable"].ToString(),
                                    latestVersion,
                                    currentVersion);
                                _logger.LogInfo(message);
                            }
                            else
                            {
                                _logger.LogInfo($"No new version available. Latest: {latestVersion}, Current: {currentVersion}");
                            }
                            return null;
                        }
                    }

                    // If we couldn't extract the version, rethrow the original exception
                    throw;
                }
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
#endif

#if NET48
                    // Extract version from tag name
                    string tagName = root["tag_name"].ToString();
                    Version latestVersion = ParseVersionFromTag(tagName);

                    // Check if the latest version is newer than the current version
                    if (latestVersion > currentVersion)
                    {
                        _logger.LogInfo($"New version available: {latestVersion} (current: {currentVersion})");

                        // Extract release information
                        string releaseNotes = root["body"].ToString();
                        string releaseUrl = root["html_url"].ToString();
                        DateTime publishedDate = DateTime.Parse(root["published_at"].ToString());

                        // Find the appropriate asset to download
                        string downloadUrl = null;
                        var assets = root["assets"] as JArray;
                        if (assets != null && assets.Count > 0)
                        {
                            foreach (var asset in assets)
                            {
                                string assetName = asset["name"].ToString();
                                if (IsMatchingAsset(assetName, applicationName))
                                {
                                    downloadUrl = asset["browser_download_url"].ToString();
                                    break;
                                }
                            }
                        }

                        // If no specific asset was found, use the zipball URL
                        if (downloadUrl == null)
                        {
                            downloadUrl = root["zipball_url"].ToString();
                        }

                        return new UpdateInfo(
                            latestVersion,
                            downloadUrl,
                            releaseUrl,
                            releaseNotes,
                            false,
                            publishedDate
                        );
                    }
                    else
                    {
                        // Use the application's language resources for this message
                        if (Application.Current.Resources.Contains("NoNewVersionAvailable"))
                        {
                            string message = string.Format(
                                Application.Current.Resources["NoNewVersionAvailable"].ToString(),
                                latestVersion,
                                currentVersion);
                            _logger.LogInfo(message);
                        }
                        else
                        {
                            _logger.LogInfo($"No new version available. Latest: {latestVersion}, Current: {currentVersion}");
                        }
                        return null;
                    }
                }
#else
                    // Extract version from tag name
                    string tagName = root.GetProperty("tag_name").GetString();
                    Version latestVersion = ParseVersionFromTag(tagName);

                    // Check if the latest version is newer than the current version
                    if (latestVersion > currentVersion)
                    {
                        _logger.LogInfo($"New version available: {latestVersion} (current: {currentVersion})");

                        // Extract release information
                        string releaseNotes = root.GetProperty("body").GetString();
                        string releaseUrl = root.GetProperty("html_url").GetString();
                        DateTime publishedDate = DateTime.Parse(root.GetProperty("published_at").GetString());

                        // Find the appropriate asset to download
                        string downloadUrl = null;
                        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var asset in assets.EnumerateArray())
                            {
                                string assetName = asset.GetProperty("name").GetString();
                                if (IsMatchingAsset(assetName, applicationName))
                                {
                                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                                    break;
                                }
                            }
                        }

                        // If no specific asset was found, use the zipball URL
                        if (downloadUrl == null)
                        {
                            downloadUrl = root.GetProperty("zipball_url").GetString();
                        }

                        return new UpdateInfo(
                            latestVersion,
                            downloadUrl,
                            releaseUrl,
                            releaseNotes,
                            false,
                            publishedDate
                        );
                    }
                    else
                    {
                        // Use the application's language resources for this message
                        if (Application.Current.Resources.Contains("NoNewVersionAvailable"))
                        {
                            string message = string.Format(
                                Application.Current.Resources["NoNewVersionAvailable"].ToString(),
                                latestVersion,
                                currentVersion);
                            _logger.LogInfo(message);
                        }
                        else
                        {
                            _logger.LogInfo($"No new version available. Latest: {latestVersion}, Current: {currentVersion}");
                        }
                        return null;
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking for updates from GitHub: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses a version from a Git tag.
        /// </summary>
        /// <param name="tagName">The tag name to parse.</param>
        /// <returns>The parsed version.</returns>
        private Version ParseVersionFromTag(string tagName)
        {
            // Remove leading 'v' if present
            if (tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                tagName = tagName.Substring(1);
            }

            // Extract version numbers using regex
            var match = Regex.Match(tagName, @"(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?");
            if (match.Success)
            {
                int major = int.Parse(match.Groups[1].Value);
                int minor = int.Parse(match.Groups[2].Value);
                int build = int.Parse(match.Groups[3].Value);
                int revision = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

                return new Version(major, minor, build, revision);
            }

            // If regex fails, try parsing directly
            if (Version.TryParse(tagName, out var version))
            {
                return version;
            }

            // Default to 0.0.0.0 if parsing fails
            return new Version(0, 0, 0, 0);
        }

        /// <summary>
        /// Determines if an asset matches the application.
        /// </summary>
        /// <param name="assetName">The name of the asset.</param>
        /// <param name="applicationName">The name of the application.</param>
        /// <returns>True if the asset matches the application; otherwise, false.</returns>
        private bool IsMatchingAsset(string assetName, string applicationName)
        {
            // Check if the asset name contains the application name (case-insensitive)
            if (assetName.IndexOf(applicationName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Check if it's an executable or zip file
                return assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                       assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
