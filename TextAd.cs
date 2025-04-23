using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    /// <summary>
    /// Represents a text advertisement with metadata
    /// </summary>
    public class TextAd
    {
        /// <summary>
        /// The text content to display
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Languages this text ad supports (empty or "all" means all languages)
        /// </summary>
        public List<string> Languages { get; set; } = new List<string>();

        /// <summary>
        /// How fast to scroll this ad (higher values = slower scrolling)
        /// </summary>
        public int Duration { get; set; } = 10;

        /// <summary>
        /// Optional URL to open when the text is clicked
        /// </summary>
#if NET6_0_OR_GREATER
        public string? Url { get; set; }
#else
        public string Url { get; set; }
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
        /// Checks if this text ad should be shown for the given language
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
}
