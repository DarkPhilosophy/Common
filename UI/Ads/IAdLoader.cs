using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.UI.Ads
{
    /// <summary>
    /// Interface for ad loaders that can load ads from various sources
    /// </summary>
    public interface IAdLoader
    {
        /// <summary>
        /// Initialize the ad loader with a logging callback
        /// </summary>
#if NET48
        void Initialize(Action<string, bool, bool, bool, bool, bool, Dictionary<string, object>> logCallback);
#else
        void Initialize(Action<string, bool, bool, bool, bool, bool, Dictionary<string, object>>? logCallback);
#endif

        /// <summary>
        /// Load ad metadata asynchronously
        /// </summary>
        Task<ImageAdMetadata> LoadAdMetadataAsync();

        /// <summary>
        /// Load text ads from a file asynchronously
        /// </summary>
        Task<List<string>> LoadTextAdsFromFileAsync();

        /// <summary>
        /// Load an image file asynchronously
        /// </summary>
#if NET48
        Task<byte[]> LoadImageFileAsync(string filename);
#else
        Task<byte[]?> LoadImageFileAsync(string filename);
#endif

        /// <summary>
        /// Convert a Unix timestamp to a human-readable date string
        /// </summary>
        string TimestampToString(long timestamp);

        /// <summary>
        /// Find an image file from the given filename asynchronously
        /// </summary>
#if NET48
        Task<string> FindImageFileAsync(string fileName);
#else
        Task<string?> FindImageFileAsync(string fileName);
#endif

        /// <summary>
        /// Get the cached metadata without loading it again
        /// </summary>
#if NET48
        ImageAdMetadata GetCachedMetadata();
#else
        ImageAdMetadata? GetCachedMetadata();
#endif
    }
}
