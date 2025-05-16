using System;
using System.IO;
using System.Net;
#if !NET48
using System.Net.Http;
using System.Text.Json;
#endif
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common.Logging;
#if NET48
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace Common.Update
{
    /// <summary>
    /// Provides update checking functionality from a custom web API.
    /// </summary>
    public class CustomWebUpdateSource : IUpdateSource
    {
#if NET48
        private static readonly WebClient _webClient = new WebClient();
#else
        private static readonly HttpClient _httpClient = new HttpClient();
#endif
        private readonly Logger _logger = Logger.Instance;
        private readonly string _updateCheckUrl;
        private readonly string _apiKey;

        /// <summary>
        /// Initializes a new instance of the CustomWebUpdateSource class.
        /// </summary>
        /// <param name="updateCheckUrl">The URL to check for updates.</param>
        /// <param name="apiKey">Optional API key for authentication.</param>
        public CustomWebUpdateSource(string updateCheckUrl, string apiKey = null)
        {
            _updateCheckUrl = updateCheckUrl ?? throw new ArgumentNullException(nameof(updateCheckUrl));
            _apiKey = apiKey;

#if NET48
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _webClient.Headers.Add("X-API-Key", _apiKey);
            }
            _webClient.Headers.Add("User-Agent", "AutoUpdater");
#endif
        }

        /// <summary>
        /// Checks if an update is available from the custom web API.
        /// </summary>
        /// <param name="applicationName">The name of the application to check for updates.</param>
        /// <param name="currentVersion">The current version of the application.</param>
        /// <returns>Update information if an update is available; otherwise, null.</returns>
        public async Task<UpdateInfo> CheckForUpdateAsync(string applicationName, Version currentVersion)
        {
            try
            {
                _logger.LogInfo($"Checking for updates for {applicationName} v{currentVersion} from custom source: {_updateCheckUrl}");

#if NET48
                _webClient.Headers["X-Application-Name"] = applicationName;
                _webClient.Headers["X-Application-Version"] = currentVersion.ToString();
                string json = await _webClient.DownloadStringTaskAsync(_updateCheckUrl);
                using (var reader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var root = JObject.Load(jsonReader);
                    if (root["updateAvailable"] != null && (bool)root["updateAvailable"])
                    {
                        string versionString = root["version"].ToString();
                        Version latestVersion = Version.Parse(versionString);
                        string downloadUrl = root["downloadUrl"].ToString();
                        string releaseUrl = root["releaseUrl"].ToString();
                        string releaseNotes = root["releaseNotes"].ToString();
                        bool isMandatory = root["isMandatory"] != null && (bool)root["isMandatory"];
                        DateTime? publishedDate = null;
                        if (root["publishedDate"] != null)
                        {
                            publishedDate = DateTime.Parse(root["publishedDate"].ToString());
                        }
                        string sha256 = "";
                        var sha256Match = Regex.Match(releaseNotes, @"SHA256:\s*([0-9A-Fa-f]{64})");
                        if (sha256Match.Success)
                        {
                            sha256 = sha256Match.Groups[1].Value;
                        }
                        return new UpdateInfo(
                            latestVersion,
                            downloadUrl,
                            releaseUrl,
                            releaseNotes,
                            sha256,
                            isMandatory,
                            publishedDate,
                            true
                        );
                    }
                    else
                    {
                        _logger.LogInfo("No new version available from custom source.");
                        return null;
                    }
                }
#else
                var request = new HttpRequestMessage(HttpMethod.Get, _updateCheckUrl);
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    request.Headers.Add("X-API-Key", _apiKey);
                }
                request.Headers.Add("X-Application-Name", applicationName);
                request.Headers.Add("X-Application-Version", currentVersion.ToString());
                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string json = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    if (root.TryGetProperty("updateAvailable", out var updateAvailable) && updateAvailable.GetBoolean())
                    {
                        string versionString = root.GetProperty("version").GetString();
                        Version latestVersion = Version.Parse(versionString);
                        string downloadUrl = root.GetProperty("downloadUrl").GetString();
                        string releaseUrl = root.GetProperty("releaseUrl").GetString();
                        string releaseNotes = root.GetProperty("releaseNotes").GetString();
                        bool isMandatory = root.GetProperty("isMandatory").GetBoolean();
                        DateTime? publishedDate = null;
                        if (root.TryGetProperty("publishedDate", out var publishedDateElement))
                        {
                            publishedDate = DateTime.Parse(publishedDateElement.GetString());
                        }
                        string sha256 = "";
                        var sha256Match = Regex.Match(releaseNotes, @"SHA256:\s*([0-9A-Fa-f]{64})");
                        if (sha256Match.Success)
                        {
                            sha256 = sha256Match.Groups[1].Value;
                        }
                        return new UpdateInfo(
                            latestVersion,
                            downloadUrl,
                            releaseUrl,
                            releaseNotes,
                            sha256,
                            isMandatory,
                            publishedDate,
                            true
                        );
                    }
                    else
                    {
                        _logger.LogInfo("No new version available from custom source.");
                        return null;
                    }
                }
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking for updates from custom source: {ex.Message}");
                return null;
            }
        }
    }
}