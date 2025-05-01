using System;
using System.Threading.Tasks;

namespace Common.Update
{
    /// <summary>
    /// Defines an interface for update sources.
    /// </summary>
    public interface IUpdateSource
    {
        /// <summary>
        /// Checks if an update is available.
        /// </summary>
        /// <param name="applicationName">The name of the application to check for updates.</param>
        /// <param name="currentVersion">The current version of the application.</param>
        /// <returns>Update information if an update is available; otherwise, null.</returns>
        Task<UpdateInfo> CheckForUpdateAsync(string applicationName, Version currentVersion);
    }
}
