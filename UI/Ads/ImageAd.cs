using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.UI.Ads
{
    /// <summary>
    /// Represents an image advertisement with metadata
    /// </summary>
    public class ImageAd
    {
        /// <summary>
        /// The filename of the image in the ads directory
        /// </summary>
        public string File { get; set; } = string.Empty;

        /// <summary>
        /// Languages this image ad supports (empty or "all" means all languages)
        /// </summary>
        public List<string> Languages { get; set; } = new List<string>();

        /// <summary>
        /// How long to display this ad in seconds
        /// </summary>
        public int Duration { get; set; } = 5;

        /// <summary>
        /// Optional URL to open when the image is clicked
        /// </summary>
#if NET6_0_OR_GREATER
        public string? Url { get; set; }
#else
        public string Url { get; set; }
#endif

        /// <summary>
        /// Optional description of the image ad
        /// </summary>
#if NET6_0_OR_GREATER
        public string? Description { get; set; }
#else
        public string Description { get; set; }
#endif

        /// <summary>
        /// Unique identifier for this ad
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Timestamp when this ad was last updated (Unix timestamp format)
        /// </summary>
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Checks if this image ad should be shown for the given language
        /// </summary>
        public bool SupportsLanguage(string language)
        {
            // If no languages specified or contains "all", show for all languages
            if (Languages.Count == 0 || Languages.Contains("all"))
                return true;

            // Otherwise, check if the language is supported (case-insensitive)
            return Languages.Any(lang => string.Equals(lang, language, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Container for ad metadata from JSON
    /// </summary>
    public class ImageAdMetadata
    {
        /// <summary>
        /// List of image ads to display
        /// </summary>
        public List<ImageAd> Images { get; set; } = new List<ImageAd>();

        /// <summary>
        /// List of text ads to display
        /// </summary>
        public List<TextAd> Texts { get; set; } = new List<TextAd>();
    }
}
