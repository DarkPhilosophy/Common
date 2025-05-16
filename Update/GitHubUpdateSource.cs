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
            _webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
#else
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

                string apiUrl = $"https://api.github.com/repos/{_owner}/{_repo}/releases/latest";
                string json;

#if NET48
                try
                {
                    json = await _webClient.DownloadStringTaskAsync(apiUrl);
                }
                catch (WebException ex) when ((ex.Response as HttpWebResponse)?.StatusCode == HttpStatusCode.Forbidden)
                {
                    _logger.LogInfo("GitHub API rate limit exceeded, trying alternative method...");
                    string releasesUrl = $"https://github.com/{_owner}/{_repo}/releases";
                    string html = await _webClient.DownloadStringTaskAsync(releasesUrl);
                    var versionMatch = Regex.Match(html, @"releases/tag/v?([0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?)");
                    if (versionMatch.Success)
                    {
                        string versionString = versionMatch.Groups[1].Value;
                        Version latestVersion = Version.Parse(versionString);
                        string releaseNotes = ExtractRawNotesFromHtml(html);
                        if (latestVersion > currentVersion)
                        {
                            _logger.LogInfo($"New version available: {latestVersion} (current: {currentVersion})");
                            return new UpdateInfo(
                                latestVersion,
                                $"https://github.com/{_owner}/{_repo}/releases/latest",
                                $"https://github.com/{_owner}/{_repo}/releases/latest",
                                releaseNotes,
                                "",
                                false,
                                DateTime.Now,
                                true
                            );
                        }
                        else
                        {
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
                    throw;
                }

                using (var reader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var root = JObject.Load(jsonReader);
#else
                try
                {
                    var response = await _httpClient.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();
                    json = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("403"))
                {
                    _logger.LogInfo("GitHub API rate limit exceeded, trying alternative method...");
                    string releasesUrl = $"https://github.com/{_owner}/{_repo}/releases";
                    var response = await _httpClient.GetAsync(releasesUrl);
                    response.EnsureSuccessStatusCode();
                    string html = await response.Content.ReadAsStringAsync();
                    var versionMatch = Regex.Match(html, @"releases/tag/v?([0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?)");
                    if (versionMatch.Success)
                    {
                        string versionString = versionMatch.Groups[1].Value;
                        Version latestVersion = Version.Parse(versionString);
                        string releaseNotes = ExtractRawNotesFromHtml(html);
                        _logger.LogInfo($"GitHubUpdateSource RAW release notes from HTML: {releaseNotes}", true);
                        if (latestVersion > currentVersion)
                        {
                            _logger.LogInfo($"New version available: {latestVersion} (current: {currentVersion})");
                            return new UpdateInfo(
                                latestVersion,
                                $"https://github.com/{_owner}/{_repo}/releases/latest",
                                $"https://github.com/{_owner}/{_repo}/releases/latest",
                                releaseNotes,
                                "",
                                false,
                                DateTime.Now,
                                true
                            );
                        }
                        else
                        {
                            if (Application.Current.Resources.Contains("NoNewVersionAvailable"))
                            {
                                string message = string.Format(
                                    Application.Current.Resources["NoNewVersionAvailable"].ToString(),
                                    latestVersion,
                                    currentVersion);
                                _logger.LogInfo(message, true);
                            }
                            else
                            {
                                _logger.LogInfo($"No new version available. Latest: {latestVersion}, Current: {currentVersion}");
                            }
                            return null;
                        }
                    }
                    throw;
                }
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
#endif
#if NET48
                    string tagName = root["tag_name"].ToString();
                    Version latestVersion = ParseVersionFromTag(tagName);
                    string releaseNotes = root["body"].ToString();
                    // Log the raw release notes with newlines in a single message
                    _logger.LogInfo($"RAW RELEASE NOTES FROM API:\n{releaseNotes}", true);
                    string releaseUrl = root["html_url"].ToString();
                    DateTime publishedDate = DateTime.Parse(root["published_at"].ToString());
                    string downloadUrl = null;
                    if (root["assets"] != null && root["assets"].Type == JTokenType.Array)
                    {
                        foreach (var asset in root["assets"])
                        {
                            string assetName = asset["name"].ToString();
                            if (IsMatchingAsset(assetName, applicationName))
                            {
                                downloadUrl = asset["browser_download_url"].ToString();
                                break;
                            }
                        }
                    }
                    if (downloadUrl == null && root["zipball_url"] != null)
                    {
                        downloadUrl = root["zipball_url"].ToString();
                    }
#else
                    string tagName = root.GetProperty("tag_name").GetString();
                    Version latestVersion = ParseVersionFromTag(tagName);
                    string releaseNotes = root.GetProperty("body").GetString();
                    // Log the raw release notes with newlines in a single message
                    _logger.LogInfo($"RAW RELEASE NOTES FROM API (.NET Core):\n{releaseNotes}", true);
                    string releaseUrl = root.GetProperty("html_url").GetString();
                    DateTime publishedDate = DateTime.Parse(root.GetProperty("published_at").GetString());
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
                    if (downloadUrl == null)
                    {
                        downloadUrl = root.GetProperty("zipball_url").GetString();
                    }
#endif

                    if (latestVersion > currentVersion)
                    {
                        _logger.LogInfo($"New version available: {latestVersion} (current: {currentVersion})");
                        return new UpdateInfo(
                            latestVersion,
                            downloadUrl,
                            releaseUrl,
                            releaseNotes,
                            "",
                            false,
                            publishedDate,
                            true
                        );
                    }
                    else
                    {
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
                            _logger.LogInfo($"No new version available. Latest: {latestVersion}, Current: {currentVersion}", true);
                        }
                        return new UpdateInfo(
                            latestVersion,
                            releaseUrl,
                            releaseUrl,
                            releaseNotes,
                            "",
                            false,
                            publishedDate,
                            false
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking for updates from GitHub: {ex.Message}", true);
                try
                {
                    string releasesUrl = $"https://github.com/{_owner}/{_repo}/releases";
                    _logger.LogInfo($"Attempting to get version directly from: {releasesUrl}");
                    try
                    {
#if NET48
                        string html = await _webClient.DownloadStringTaskAsync(releasesUrl);
#else
                        var response = await _httpClient.GetAsync(releasesUrl);
                        response.EnsureSuccessStatusCode();
                        string html = await response.Content.ReadAsStringAsync();
#endif
                        var versionMatch = Regex.Match(html, @"releases/tag/v?([0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?)");
                        if (versionMatch.Success)
                        {
                            string versionString = versionMatch.Groups[1].Value;
                            _logger.LogInfo($"Extracted raw version string from HTML: '{versionString}'");
                            Version latestVersion = Version.Parse(versionString);
                            _logger.LogInfo($"Successfully extracted version {latestVersion} from HTML page");
                            string releaseNotes = ExtractRawNotesFromHtml(html);
                            // Log the raw release notes
                            _logger.LogInfo($"RAW RELEASE NOTES FROM HTML:\n{releaseNotes}", true);
                            bool updateNeeded = latestVersion > currentVersion;
                            if (Application.Current.Resources.Contains("NoNewVersionAvailable") && !updateNeeded)
                            {
                                string message = string.Format(
                                    Application.Current.Resources["NoNewVersionAvailable"].ToString(),
                                    latestVersion,
                                    currentVersion);
                                _logger.LogInfo(message);
                            }
                            else
                            {
                                _logger.LogInfo($"Latest version from HTML: {latestVersion}, Current: {currentVersion}, Update needed: {updateNeeded}");
                            }
                            return new UpdateInfo(
                                latestVersion,
                                releasesUrl,
                                releasesUrl,
                                releaseNotes,
                                "",
                                false,
                                DateTime.Now,
                                updateNeeded
                            );
                        }
                    }
                    catch (Exception htmlEx)
                    {
                        _logger.LogInfo($"Failed to extract version from HTML: {htmlEx.Message}");
                    }
                    _logger.LogInfo("Could not determine version from GitHub, returning null");
                    return null;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError($"Error creating fallback update info: {fallbackEx.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Cleans up release notes by removing headers, metadata, and formatting.
        /// </summary>
        /// <param name="releaseNotes">The raw release notes to clean.</param>
        /// <returns>Cleaned release notes.</returns>
        private string CleanReleaseNotes(string releaseNotes)
        {
            if (string.IsNullOrWhiteSpace(releaseNotes))
                return releaseNotes;

            _logger.LogInfo($"Cleaning release notes: {releaseNotes}", true);

            // Remove any Markdown headers (# and ##)
            releaseNotes = Regex.Replace(releaseNotes, @"^#+ .*$", "", RegexOptions.Multiline).Trim();

            // Remove Build Date and SHA256 lines
            releaseNotes = Regex.Replace(releaseNotes, @"^Build Date:.*$", "", RegexOptions.Multiline).Trim();
            releaseNotes = Regex.Replace(releaseNotes, @"^SHA256:.*$", "", RegexOptions.Multiline).Trim();

            // Remove "Changes" header
            releaseNotes = Regex.Replace(releaseNotes, @"^Changes$", "", RegexOptions.Multiline).Trim();

            // Normalize line endings
            releaseNotes = releaseNotes.Replace("\r\n", "\n").Replace("\r", "\n");

            // Remove multiple consecutive empty lines
            releaseNotes = Regex.Replace(releaseNotes, @"\n{3,}", "\n\n");

            _logger.LogInfo($"Cleaned release notes: {releaseNotes}", true);
            return releaseNotes;
        }

        private string ExtractRawNotesFromHtml(string html)
        {
            // Log a snippet of the raw HTML for debugging
            _logger.LogInfo($"HTML snippet (first 1000 chars):\n{html.Substring(0, Math.Min(1000, html.Length))}...", true);

            var releaseNotesMatch = Regex.Match(html, @"<div class=""markdown-body[^""]*"">(.*?)</div>\s*</div>\s*</div>", RegexOptions.Singleline);
            if (releaseNotesMatch.Success)
            {
                string rawNotes = releaseNotesMatch.Groups[1].Value;

                // Log the raw notes with HTML tags
                _logger.LogInfo($"Raw notes with HTML tags:\n{rawNotes}", true);

                // Just remove HTML tags and nothing else
                rawNotes = Regex.Replace(rawNotes, @"<[^>]+>", "").Trim();

                // Log the raw notes after HTML tags removed
                _logger.LogInfo($"Raw notes after HTML tags removed:\n{rawNotes}", true);

                return rawNotes;
            }
            return "No release notes available.";
        }

        private Version ParseVersionFromTag(string tagName)
        {
            if (tagName.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                tagName = tagName.Substring(1);
            }
            var match = Regex.Match(tagName, @"(\d+)\.(\d+)\.(\d+)(?:\.(\d+))?");
            if (match.Success)
            {
                int major = int.Parse(match.Groups[1].Value);
                int minor = int.Parse(match.Groups[2].Value);
                int build = int.Parse(match.Groups[3].Value);
                int revision = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
                return new Version(major, minor, build, revision);
            }
            if (Version.TryParse(tagName, out var version))
            {
                return version;
            }
            return new Version(0, 0, 0, 0);
        }

        private bool IsMatchingAsset(string assetName, string applicationName)
        {
            if (assetName.IndexOf(applicationName, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return assetName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                       assetName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }
    }
}