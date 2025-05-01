using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Common.Update
{
    /// <summary>
    /// Provides UI helpers for the AutoUpdater in WPF applications.
    /// </summary>
    public static class UpdateUI
    {
        /// <summary>
        /// Checks for updates and shows a dialog if an update is available.
        /// </summary>
        /// <param name="updater">The AutoUpdater instance.</param>
        /// <param name="owner">The owner window for the dialog.</param>
        /// <returns>True if an update was found and installed; otherwise, false.</returns>
        public static async Task<bool> CheckForUpdatesWithUIAsync(this AutoUpdater updater, Window owner = null)
        {
            try
            {
                // Check for updates
                var updateInfo = await updater.CheckForUpdateAsync();
                if (updateInfo == null)
                    return false;

                // Try to get changelog from GitHub
                string changelog = GetChangelogFromGitHub(updateInfo.ReleaseUrl);
                
                // Prepare message content
                string message;
                if (!string.IsNullOrEmpty(changelog))
                {
                    // Use changelog if available (limit to 500 chars to avoid huge dialog)
                    string truncatedChangelog = changelog.Length > 500 
                        ? changelog.Substring(0, 500) + "...\n(See full changelog after update)" 
                        : changelog;
                    
                    message = $"A new version of the application is available: v{updateInfo.Version}\n\n" +
                              $"Current version: v{typeof(UpdateUI).Assembly.GetName().Version}\n\n" +
                              $"Changelog:\n{truncatedChangelog}\n\n" +
                              "Do you want to download and install this update now?";
                }
                else
                {
                    // Fall back to release notes
                    message = $"A new version of the application is available: v{updateInfo.Version}\n\n" +
                              $"Current version: v{typeof(UpdateUI).Assembly.GetName().Version}\n\n" +
                              $"Release notes:\n{updateInfo.ReleaseNotes}\n\n" +
                              "Do you want to download and install this update now?";
                }
                
                // Show update dialog
                var result = MessageBox.Show(
                    owner,
                    message,
                    "Update Available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    // Create progress dialog
                    var progressDialog = new ProgressDialog("Downloading Update", "Please wait while the update is being downloaded...");
                    progressDialog.Owner = owner;
                    
                    // Create a progress reporter
                    var progress = new Progress<int>(percent => {
                        progressDialog.UpdateProgress(percent);
                    });
                    
                    // Start the download in the background
                    var downloadTask = Task.Run(async () => {
                        try {
                            return await updater.DownloadAndInstallUpdateAsync(updateInfo);
                        }
                        finally {
                            // Close the dialog when download completes
                            Application.Current.Dispatcher.Invoke(() => {
                                if (progressDialog.IsVisible) {
                                    progressDialog.Close();
                                }
                            });
                        }
                    });
                    
                    // Show the dialog
                    progressDialog.ShowDialog();
                    
                    // Return the result of the download
                    return await downloadTask;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    owner,
                    $"An error occurred while checking for updates: {ex.Message}",
                    "Update Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }
        }
        
        /// <summary>
        /// Attempts to get the CHANGELOG.md content from GitHub
        /// </summary>
        /// <param name="releaseUrl">The GitHub release URL</param>
        /// <returns>The changelog content or null if not found</returns>
        private static string GetChangelogFromGitHub(string releaseUrl)
        {
            try
            {
                // Extract owner and repo from the release URL
                // Example: https://github.com/DarkPhilosophy/ConfigReplacer/releases/tag/v1.0.0
                Uri uri = new Uri(releaseUrl);
                string path = uri.AbsolutePath;
                string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (segments.Length >= 2)
                {
                    string owner = segments[0];
                    string repo = segments[1];
                    
                    // Try to get CHANGELOG.md from the repository
                    string changelogUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/main/CHANGELOG.md";
                    
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                        try
                        {
                            return client.DownloadString(changelogUrl);
                        }
                        catch
                        {
                            // Try master branch if main doesn't exist
                            changelogUrl = $"https://raw.githubusercontent.com/{owner}/{repo}/master/CHANGELOG.md";
                            try
                            {
                                return client.DownloadString(changelogUrl);
                            }
                            catch
                            {
                                // Changelog not found
                                return null;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Any error, just return null
            }
            
            return null;
        }
    }
    
    /// <summary>
    /// A simple progress dialog for WPF applications.
    /// </summary>
    public class ProgressDialog : Window
    {
        private ProgressBar _progressBar;
        private TextBlock _messageTextBlock;
        private TextBlock _percentTextBlock;
        
        /// <summary>
        /// Initializes a new instance of the ProgressDialog class.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="message">The message to display.</param>
        public ProgressDialog(string title, string message)
        {
            Title = title;
            Width = 400;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.ToolWindow;
            ResizeMode = ResizeMode.NoResize;

            // Create the content
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Add the message
            _messageTextBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            grid.Children.Add(_messageTextBlock);
            Grid.SetRow(_messageTextBlock, 0);

            // Add the progress bar
            _progressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Margin = new Thickness(10, 5, 10, 5),
                Height = 20,
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            grid.Children.Add(_progressBar);
            Grid.SetRow(_progressBar, 1);
            
            // Add percentage text
            _percentTextBlock = new TextBlock
            {
                Text = "Starting...",
                Margin = new Thickness(10, 0, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            grid.Children.Add(_percentTextBlock);
            Grid.SetRow(_percentTextBlock, 2);

            Content = grid;
        }
        
        /// <summary>
        /// Updates the progress bar and percentage text.
        /// </summary>
        /// <param name="percent">The percentage of completion (0-100).</param>
        public void UpdateProgress(int percent)
        {
            if (percent >= 0 && percent <= 100)
            {
                Dispatcher.Invoke(() =>
                {
                    _progressBar.IsIndeterminate = false;
                    _progressBar.Value = percent;
                    _percentTextBlock.Text = $"{percent}% complete";
                    
                    if (percent == 100)
                    {
                        _messageTextBlock.Text = "Download complete. Installing update...";
                    }
                });
            }
        }
    }
}
