using System;

namespace Common.Update
{
    /// <summary>
    /// Contains information about an available update.
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// Gets the version of the update.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Gets the URL to download the update.
        /// </summary>
        public string DownloadUrl { get; }

        /// <summary>
        /// Gets the URL to the release page.
        /// </summary>
        public string ReleaseUrl { get; }

        /// <summary>
        /// Gets the release notes for the update.
        /// </summary>
        public string ReleaseNotes { get; }

        /// <summary>
        /// Gets a value indicating whether the update is mandatory.
        /// </summary>
        public bool IsMandatory { get; }

        /// <summary>
        /// Gets the date the update was published.
        /// </summary>
        public DateTime PublishedDate { get; }

        /// <summary>
        /// Initializes a new instance of the UpdateInfo class.
        /// </summary>
        /// <param name="version">The version of the update.</param>
        /// <param name="downloadUrl">The URL to download the update.</param>
        /// <param name="releaseUrl">The URL to the release page.</param>
        /// <param name="releaseNotes">The release notes for the update.</param>
        /// <param name="isMandatory">Whether the update is mandatory.</param>
        /// <param name="publishedDate">The date the update was published.</param>
        public UpdateInfo(
            Version version,
            string downloadUrl,
            string releaseUrl,
            string releaseNotes = "",
            bool isMandatory = false,
            DateTime? publishedDate = null)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            DownloadUrl = downloadUrl ?? throw new ArgumentNullException(nameof(downloadUrl));
            ReleaseUrl = releaseUrl ?? throw new ArgumentNullException(nameof(releaseUrl));
            ReleaseNotes = releaseNotes ?? string.Empty;
            IsMandatory = isMandatory;
            PublishedDate = publishedDate ?? DateTime.UtcNow;
        }
    }
}
