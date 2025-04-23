using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Interface for ad loaders that can load ads from various sources
    /// </summary>
    public interface IAdLoader
    {
        /// <summary>
        /// Initialize the ad loader with a logging callback
        /// </summary>
#if NET6_0_OR_GREATER
        void Initialize(Action<string, bool, bool, bool, bool, bool>? logCallback);
#else
        void Initialize(Action<string, bool, bool, bool, bool, bool> logCallback);
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
#if NET6_0_OR_GREATER
        Task<byte[]?> LoadImageFileAsync(string filename);
#else
        Task<byte[]> LoadImageFileAsync(string filename);
#endif

        /// <summary>
        /// Convert a Unix timestamp to a human-readable date string
        /// </summary>
        string TimestampToString(long timestamp);

        /// <summary>
        /// Find an image file from the given filename asynchronously
        /// </summary>
#if NET6_0_OR_GREATER
        Task<string?> FindImageFileAsync(string fileName);
#else
        Task<string> FindImageFileAsync(string fileName);
#endif

        /// <summary>
        /// Get the cached metadata without loading it again
        /// </summary>
#if NET6_0_OR_GREATER
        ImageAdMetadata? GetCachedMetadata();
#else
        ImageAdMetadata GetCachedMetadata();
#endif
    }
}
