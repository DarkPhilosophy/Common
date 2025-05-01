using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Common.Logging;

namespace Common.UI.Ads
{
    public class AdManager
    {
#if NET6_0_OR_GREATER
        private static AdManager? _instance;
#else
        private static AdManager _instance;
#endif
        private readonly DispatcherTimer _timer;
#if NET6_0_OR_GREATER
        private TextBlock? _adBanner;
#else
        private TextBlock _adBanner;
#endif
#if NET6_0_OR_GREATER
        private Grid? _adContainer;
#else
        private Grid _adContainer;
#endif
#if NET6_0_OR_GREATER
        private Border? _adBannerContainer;
#else
        private Border _adBannerContainer;
#endif
        private List<string> _adMessages = new List<string>();
        private List<List<(string text, string color)>> _parsedAdMessages = new List<List<(string text, string color)>>();
        private List<double> _messageWidths = new List<double>();
        private int _currentAdIndex = 0;
        private int _bannerWidth = 0;
#if NET6_0_OR_GREATER
        private Action<string, bool, bool, bool, bool, bool>? _logCallback;
#else
        private Action<string, bool, bool, bool, bool, bool> _logCallback;
#endif
        private string _currentLanguage = "English";
        private Dictionary<string, List<string>> _languageMessages = new Dictionary<string, List<string>>();
        private List<string> _universalMessages = new List<string>();
        private Dictionary<string, string> _textAdUrls = new Dictionary<string, string>();
        private List<int> _textAdDurations = new List<int>();
        private Dictionary<string, int> _messageDurations = new Dictionary<string, int>();
#if NET6_0_OR_GREATER
        private CancellationTokenSource? _globalCts;
#else
        private CancellationTokenSource _globalCts;
#endif
        private List<ImageAd> _imageAds = new List<ImageAd>();
        private int _currentImageAdIndex = 0;
#if NET6_0_OR_GREATER
        private Image? _currentImageControl;
#else
        private Image _currentImageControl;
#endif
#if NET6_0_OR_GREATER
        private DispatcherTimer? _imageTimer;
#else
        private DispatcherTimer _imageTimer;
#endif
        private bool _isTransitioning = false;
        private Dictionary<string, BitmapImage> _imageCache = new Dictionary<string, BitmapImage>();
#if NET6_0_OR_GREATER
        private IAdLoader? _adLoader;
#else
        private IAdLoader _adLoader;
#endif
        private TranslateTransform _textTransform = new TranslateTransform();

#if NET6_0_OR_GREATER
        public static AdManager Instance => _instance ??= new AdManager();
#else
        public static AdManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AdManager();
                }
                return _instance;
            }
        }
#endif

        private AdManager()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += Timer_Tick;
            _globalCts = new CancellationTokenSource();
            Application.Current.Exit += (s, e) => Cleanup();
        }

        private void Cleanup()
        {
            Log("Cleaning up AdManager", true);
            _timer.Stop();
            _imageTimer?.Stop();
            _globalCts?.Cancel();
            _globalCts?.Dispose();
            _imageCache.Clear();
            _adMessages.Clear();
            _parsedAdMessages.Clear();
            _messageWidths.Clear();
            _languageMessages.Clear();
            _universalMessages.Clear();
            _textAdUrls.Clear();
            _messageDurations.Clear();
            _imageAds.Clear();
            Log("Cleanup complete", true);
        }

#if NET6_0_OR_GREATER
        public void Initialize(TextBlock adBanner, Grid? adContainer = null, Action<string, bool, bool, bool, bool, bool>? logCallback = null, IAdLoader? adLoader = null)
#else
        public void Initialize(TextBlock adBanner, Grid adContainer = null, Action<string, bool, bool, bool, bool, bool> logCallback = null, IAdLoader adLoader = null)
#endif
        {
            _adBanner = adBanner;
            _adContainer = adContainer ?? (adBanner.Parent as FrameworkElement)?.FindName("adContainer") as Grid;
            if (_adContainer == null && adBanner.Parent is FrameworkElement parent)
            {
                _adContainer = parent as Grid ?? parent.FindVisualChildren<Grid>().FirstOrDefault();
            }

            // Find the adBannerContainer (Border that contains the adBanner)
            _adBannerContainer = adBanner.Parent as Border;
            if (_adBannerContainer == null)
            {
                // Try to find it by name in the visual tree
                var window = Window.GetWindow(adBanner);
                if (window != null)
                {
                    _adBannerContainer = window.FindName("adBannerContainer") as Border;
                    Log($"Found adBannerContainer by name: {_adBannerContainer != null}", true);
                }
            }
            _logCallback = logCallback;
            _adLoader = adLoader;

            if (_adContainer == null)
                Log("Warning: No ad container found. Falling back to adBanner.Parent width.", true);
            if (_adBanner == null)
            {
                Log("Error: adBanner is null", true);
                return;
            }

            _adBanner.Visibility = Visibility.Visible;
            _adBanner.TextWrapping = TextWrapping.NoWrap;
            _adBanner.HorizontalAlignment = HorizontalAlignment.Left;
            _adBanner.ClipToBounds = true;
            _adBanner.RenderTransform = _textTransform;

            _adBanner.MouseDown += (s, e) =>
            {
#if NET6_0_OR_GREATER
                if (_currentAdIndex < _adMessages.Count && _textAdUrls.TryGetValue(_adMessages[_currentAdIndex], out string? url) && !string.IsNullOrEmpty(url))
#else
                string url;
                if (_currentAdIndex < _adMessages.Count && _textAdUrls.TryGetValue(_adMessages[_currentAdIndex], out url) && !string.IsNullOrEmpty(url))
#endif
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                        Log($"Opened URL: {url}", true);
                    }
                    catch (Exception ex)
                    {
                        Log($"URL error: {ex.Message}", true);
                    }
                }
            };

            _adBanner.MouseEnter += (s, e) =>
            {
                _adBanner.Cursor = _currentAdIndex < _adMessages.Count && _textAdUrls.ContainsKey(_adMessages[_currentAdIndex]) ? Cursors.Hand : Cursors.Arrow;
            };

            _adBanner.SizeChanged += (s, e) =>
            {
                double containerWidth = _adContainer?.ActualWidth ?? (_adBanner.Parent as FrameworkElement)?.ActualWidth ?? 500;
                double actualBannerWidth = e.NewSize.Width;
                _bannerWidth = (int)containerWidth;
                Log($"Banner resized: containerWidth={containerWidth}px, actualBannerWidth={actualBannerWidth}px, bannerWidth={_bannerWidth}px, parentType={_adBanner.Parent?.GetType().Name}", true);
                if (_parsedAdMessages.Count > 0)
                    ParseColorMarkdown();
            };

            _adBanner.UpdateLayout();
            double initialContainerWidth = _adContainer?.ActualWidth ?? (_adBanner.Parent as FrameworkElement)?.ActualWidth ?? 500;
            Log($"adBanner initialized: IsLoaded={_adBanner.IsLoaded}, ActualWidth={_adBanner.ActualWidth}, ContainerWidth={initialContainerWidth}, FontSize={_adBanner.FontSize}, ParentType={_adBanner.Parent?.GetType().Name}", true);

            if (!_adBanner.IsLoaded)
            {
                _adBanner.Loaded += (s, e) =>
                {
                    Log("adBanner Loaded event fired", true);
                    LoadImageAds();
                    LoadAdMessages();
                };
            }
            else
            {
                LoadImageAds();
                LoadAdMessages();
            }
        }

        private void Log(string message, bool consoleOnly)
        {
            if (_logCallback != null)
            {
                if (Application.Current != null &&
                    Application.Current.Dispatcher != null &&
                    !Application.Current.Dispatcher.CheckAccess())
                {
                    // We're not on the UI thread, so dispatch to the UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _logCallback($"AdManager: {message}", false, false, false, true, consoleOnly);
                    });
                }
                else
                {
                    // We're already on the UI thread, so call directly
                    _logCallback($"AdManager: {message}", false, false, false, true, consoleOnly);
                }
            }
        }

        private async Task LoadAdMessagesAsync()
        {
            if (_adLoader == null)
            {
                Log("No ad loader. Skipping text ads.", true);
                return;
            }

            try
            {
                // Create local variables to store data before updating UI
                Dictionary<string, List<string>> languageMessages = new Dictionary<string, List<string>>();
                List<string> universalMessages = new List<string>();
                Dictionary<string, int> messageDurations = new Dictionary<string, int>();
                Dictionary<string, string> textAdUrls = new Dictionary<string, string>();

                var metadata = await _adLoader.LoadAdMetadataAsync();
                if (metadata?.Texts?.Count > 0)
                {
                    foreach (var ad in metadata.Texts)
                    {
                        int duration = Math.Max(5, ad.Duration);
                        messageDurations[ad.Description] = duration;
                        if (ad.Languages.Count == 0 || ad.Languages.Contains("all"))
                        {
                            universalMessages.Add(ad.Description);
                        }
                        else
                        {
                            foreach (var lang in ad.Languages)
                            {
                                if (!languageMessages.ContainsKey(lang))
                                    languageMessages[lang] = new List<string>();
                                languageMessages[lang].Add(ad.Description);
                            }
                        }
                        if (!string.IsNullOrEmpty(ad.Url))
                            textAdUrls[ad.Description] = ad.Url;
                    }
                    Log($"Loaded {metadata.Texts.Count} text ads", true);
                }
                else
                {
                    var lines = await _adLoader.LoadTextAdsFromFileAsync();
                    if (lines.Count > 0)
                    {
                        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
                        {
                            if (line.StartsWith("[") && line.Contains("]"))
                            {
                                int idx = line.IndexOf(']');
                                string lang = line.Substring(1, idx - 1).Trim();
                                string msg = line.Substring(idx + 1).Trim();
                                if (!string.IsNullOrEmpty(msg))
                                {
                                    messageDurations[msg] = 15;
                                    if (lang.Equals("all", StringComparison.OrdinalIgnoreCase))
                                        universalMessages.Add(msg);
                                    else
                                    {
                                        if (!languageMessages.ContainsKey(lang))
                                            languageMessages[lang] = new List<string>();
                                        languageMessages[lang].Add(msg);
                                    }
                                }
                            }
                            else
                            {
                                universalMessages.Add(line);
                                messageDurations[line] = 15;
                            }
                        }
                        Log($"Loaded {lines.Count} ads from ads.txt", true);
                    }
                    else
                    {
                        Log("No text ads found", true);
                    }
                }

                // Update UI-related collections on the UI thread
                if (Application.Current != null && Application.Current.Dispatcher != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        _languageMessages.Clear();
                        _universalMessages.Clear();
                        _adMessages.Clear();
                        _textAdDurations.Clear();
                        _messageDurations.Clear();
                        _textAdUrls.Clear();

                        // Copy data from local variables to UI collections
                        foreach (var kvp in languageMessages)
                        {
                            _languageMessages[kvp.Key] = new List<string>(kvp.Value);
                        }
                        _universalMessages.AddRange(universalMessages);
                        foreach (var kvp in messageDurations)
                        {
                            _messageDurations[kvp.Key] = kvp.Value;
                        }
                        foreach (var kvp in textAdUrls)
                        {
                            _textAdUrls[kvp.Key] = kvp.Value;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Load error: {ex.Message}", true);
            }
        }

        private void LoadAdMessages()
        {
            Task.Run(async () =>
            {
                await LoadAdMessagesAsync();
                if (_adBanner != null)
                {
                    await _adBanner.Dispatcher.InvokeAsync(() =>
                    {
                        UpdateCurrentMessages();
                        ParseColorMarkdown();
                        if (_adMessages.Count > 0)
                            StartAnimation();
                    });
                }
            });
        }

        private void UpdateCurrentMessages()
        {
            _adMessages.Clear();
            _textAdDurations.Clear();

            if (_languageMessages.ContainsKey(_currentLanguage))
            {
                foreach (var msg in _languageMessages[_currentLanguage])
                {
                    _adMessages.Add(msg);
                    _textAdDurations.Add(_messageDurations.ContainsKey(msg) ? _messageDurations[msg] : 15);
                }
            }

            foreach (var msg in _universalMessages)
            {
                _adMessages.Add(msg);
                _textAdDurations.Add(_messageDurations.ContainsKey(msg) ? _messageDurations[msg] : 15);
            }

            // No default message if no ads are available
            // If no ads are found, the banner will remain empty

            for (int i = 0; i < _adMessages.Count; i++)
            {
                Log($"Message {i}: Duration={_textAdDurations[i]}s, Text={_adMessages[i].Substring(0, Math.Min(30, _adMessages[i].Length))}...", true);
            }

            Log($"Updated messages for {_currentLanguage}: {_adMessages.Count} ads", true);
        }

        private void ParseColorMarkdown()
        {
            _parsedAdMessages.Clear();
            _messageWidths.Clear();

            if (_adBanner == null)
            {
                Log("Cannot parse markdown: adBanner is null", true);
                return;
            }

            foreach (var message in _adMessages)
            {
                var parsed = new List<(string text, string color)>();
                StringBuilder plainText = new StringBuilder();
                int start = 0;

                while (start < message.Length)
                {
                    int tagStart = message.IndexOf("#[", start);
                    if (tagStart == -1)
                    {
                        string text = message.Substring(start);
                        parsed.Add((text, "Black"));
                        plainText.Append(text);
                        break;
                    }

                    if (tagStart > start)
                    {
                        string text = message.Substring(start, tagStart - start);
                        parsed.Add((text, "Black"));
                        plainText.Append(text);
                    }

                    int tagEnd = message.IndexOf("]", tagStart);
                    if (tagEnd == -1)
                        break;

                    string color = message.Substring(tagStart + 2, tagEnd - tagStart - 2);
                    int textEnd = message.IndexOf("#", tagEnd + 1);
                    if (textEnd == -1)
                        break;

                    string coloredText = message.Substring(tagEnd + 1, textEnd - tagEnd - 1);
                    parsed.Add((coloredText, color));
                    plainText.Append(coloredText);
                    start = textEnd + 1;
                }

                double totalWidth = 0;
                var formattedText = new FormattedText(
                    plainText.ToString(),
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(_adBanner.FontFamily, _adBanner.FontStyle, FontWeights.SemiBold, _adBanner.FontStretch),
                    _adBanner.FontSize,
                    Brushes.Black,
                    VisualTreeHelper.GetDpi(_adBanner).PixelsPerDip);
                totalWidth = formattedText.WidthIncludingTrailingWhitespace;

                if (totalWidth <= 0)
                {
                    totalWidth = plainText.Length * (_adBanner.FontSize / 1.5);
                    Log($"Warning: Fallback width used for message: {plainText}, width={totalWidth}px", true);
                }

                _parsedAdMessages.Add(parsed);
                _messageWidths.Add(totalWidth);
                Log($"Parsed message: width={totalWidth}px, text={plainText}", true);
            }
        }

        public void StartAnimation()
        {
            if (_adBanner == null)
            {
                Log("Cannot start animation: missing banner", true);
                return;
            }

            if (_parsedAdMessages.Count == 0)
            {
                Log("No ad messages available, hiding banner", true);
                _adBanner.Visibility = Visibility.Collapsed;
                if (_adBannerContainer != null)
                {
                    _adBannerContainer.Visibility = Visibility.Collapsed;
                    Log("Hiding adBannerContainer", true);
                }
                return;
            }

            // Show the banner since we have messages to display
            _adBanner.Visibility = Visibility.Visible;
            if (_adBannerContainer != null)
            {
                _adBannerContainer.Visibility = Visibility.Visible;
                Log("Showing adBannerContainer", true);
            }

            _bannerWidth = (int)(_adContainer?.ActualWidth ?? (_adBanner.Parent as FrameworkElement)?.ActualWidth ?? 500);
            _adBanner.UpdateLayout();
            _currentAdIndex = 0;
            Log($"Starting animation: bannerWidth={_bannerWidth}, IsLoaded={_adBanner.IsLoaded}, Messages={_parsedAdMessages.Count}, ContainerWidth={_adContainer?.ActualWidth ?? 0}, ParentType={_adBanner.Parent?.GetType().Name}", true);
            AnimateCurrentAd();
            if (!_timer.IsEnabled)
            {
                _timer.Start();
                Log("Animation started", true);
            }
        }

        public void StopAnimation()
        {
            _timer.Stop();
            _textTransform.BeginAnimation(TranslateTransform.XProperty, null);
            _adBanner?.BeginAnimation(UIElement.OpacityProperty, null);
            Log("Animation stopped", true);
        }

#if NET6_0_OR_GREATER
        private void Timer_Tick(object? sender, EventArgs e)
#else
        private void Timer_Tick(object sender, EventArgs e)
#endif
        {
            if (!_textTransform.HasAnimatedProperties)
            {
                _currentAdIndex = (_currentAdIndex + 1) % _parsedAdMessages.Count;
                AnimateCurrentAd();
            }
        }

        private void AnimateCurrentAd()
        {
            if (_adBanner == null || _currentAdIndex >= _parsedAdMessages.Count)
            {
                Log("Cannot animate: adBanner is null or invalid index", true);
                return;
            }

            try
            {
                var message = _parsedAdMessages[_currentAdIndex];
                _adBanner.Inlines.Clear();

                foreach (var (text, color) in message)
                {
                    var run = new Run(text)
                    {
                        FontWeight = FontWeights.SemiBold,
                        Foreground = string.IsNullOrEmpty(color) || color == "White"
                            ? new SolidColorBrush(Colors.Black)
                            : new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))
                    };
                    _adBanner.Inlines.Add(run);
                }

                _adBanner.UpdateLayout();
                int duration = _currentAdIndex < _textAdDurations.Count ? Math.Max(1, _textAdDurations[_currentAdIndex]) : 15;
                double textWidth = _messageWidths[_currentAdIndex];
                double padding = 200;
                double bannerCenter = _bannerWidth / 2.0;
                double fromX = _bannerWidth + padding; // Start just off-screen
                double toX = -(textWidth + padding); // End with text off-screen
                double totalDistance = fromX - toX;

                // Center text in slow phase
                double x2 = bannerCenter - textWidth / 2; // Text center at bannerCenter
                double textCenterX = x2 + textWidth / 2;

                // Calculate base animation duration inversely proportional to duration
                double referenceSpeed = 3000.0; // Tuned for duration=15s -> ~200px/s
                double speed = referenceSpeed / Math.Max(1, duration);
#if NET6_0_OR_GREATER
                speed = Math.Clamp(speed, 3, 800); // Allow very slow speeds
#else
                speed = Math.Max(3, Math.Min(speed, 800)); // Allow very slow speeds
#endif
                double baseDuration = totalDistance / speed;

                // Define keyframe animation for smooth fast-slow-fast effect
                var animation = new DoubleAnimationUsingKeyFrames
                {
                    Duration = TimeSpan.FromSeconds(baseDuration + 0.5) // Add 0.5s for fade
                };

                // Keyframes with splines for smooth fast-slow-fast
                double t0 = 0.0; // Start
                double t1 = 0.5 * baseDuration; // 50% time (slow middle)
                double t2 = baseDuration; // End

                double x1 = x2; // Middle: text centered
                double x0 = fromX; // Start
                double x3 = toX; // End

                animation.KeyFrames.Add(new SplineDoubleKeyFrame(x0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t0)), new KeySpline(0.2, 0, 0.8, 1)));
                animation.KeyFrames.Add(new SplineDoubleKeyFrame(x1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t1)), new KeySpline(0.2, 0, 0.8, 1)));
                animation.KeyFrames.Add(new SplineDoubleKeyFrame(x3, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(t2)), new KeySpline(0.2, 0, 0.8, 1)));

                // Estimate visibility time (when x <= bannerWidth)
                double visibilityTime = t1 * (fromX - _bannerWidth) / (fromX - x1); // Linear approximation

                var fadeIn = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(300)
                };

                animation.Completed += (s, e) =>
                {
                    var fadeOut = new DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(300)
                    };
                    fadeOut.Completed += (s2, e2) =>
                    {
                        Task.Delay(1000).ContinueWith(_ =>
                        {
                            _adBanner?.Dispatcher.Invoke(() =>
                            {
                                if (!_timer.IsEnabled)
                                    return;
                                _currentAdIndex = (_currentAdIndex + 1) % _parsedAdMessages.Count;
                                AnimateCurrentAd();
                            });
                        });
                    };
                    _adBanner.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                };

                _adBanner.Opacity = 0;
                try
                {
                    _adBanner.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                }
                catch (Exception ex)
                {
                    Log($"Opacity animation error (non-critical): {ex.Message}", true);
                    // Continue without animation - set opacity directly
                    _adBanner.Opacity = 1;
                }
                try
                {
                    _textTransform.BeginAnimation(TranslateTransform.XProperty, animation);
                }
                catch (Exception ex)
                {
                    Log($"Animation error (non-critical): {ex.Message}", true);
                    // Continue without animation - the text will still be visible
                }
                Log($"Animating ad {_currentAdIndex}: width={textWidth}px, fromX={fromX}, toX={toX}, duration={baseDuration + 0.5:F2}s, avg_speed={speed:F2}px/s, metadata_duration={duration}s, padding={padding}px, bannerWidth={_bannerWidth}px, visibility_time={visibilityTime:F2}s, text_center_x={textCenterX:F2}px, containerWidth={_adContainer?.ActualWidth ?? 0}, parentType={_adBanner.Parent?.GetType().Name}, keyframes=[t1={t1:F2}s,x1={x1:F2}]", true);
            }
            catch (Exception ex)
            {
                Log($"Animation error: {ex.Message}", true);
                _adBanner.Inlines.Clear();
                _adBanner.Visibility = Visibility.Collapsed;
            }
        }

        public void SwitchLanguage(string language)
        {
            if (_currentLanguage != language)
            {
                _currentLanguage = language;
                Log($"Switched to {language}", true);
                UpdateCurrentMessages();
                ParseColorMarkdown();
                _currentAdIndex = 0;
                UpdateImageAdsForLanguage();
                StartAnimation();
            }
        }

        private void LoadImageAds()
        {
            var uiDispatcher = _adContainer?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            // Start a background task to load ads asynchronously
            Task.Run(async () => {
                try
                {
                    Log($"Current directory: {Directory.GetCurrentDirectory()}", true);
                    if (_adLoader == null)
                    {
                        Log("No ad loader provided. Image ads will not be loaded.", true);
                        return;
                    }

                    Log("Starting asynchronous ad metadata loading", true);
                    var metadata = await _adLoader.LoadAdMetadataAsync();
                    try
                    {
                        if (metadata != null && metadata.Images != null && metadata.Images.Count > 0)
                        {
                            _imageAds = metadata.Images;
                            Log($"Loaded {_imageAds.Count} image ads from metadata", true);
                            foreach (var ad in _imageAds)
                            {
                                Log($"Image ad: {ad.File} (ID: {ad.Id}, Last Updated: {_adLoader.TimestampToString(ad.Timestamp)})", true);
                            }
                            await PreloadImagesAsync(uiDispatcher);
                            if (_imageAds.Count > 0 && _adContainer != null)
                            {
                                // Show the image ad container since we have ads to display
                                await uiDispatcher.InvokeAsync(() =>
                                {
                                    _adContainer.Visibility = Visibility.Visible;
                                    Log("Image ad container shown", true);
                                });
                                if (_imageTimer == null)
                                {
                                    _imageTimer = new DispatcherTimer
                                    {
                                        Interval = TimeSpan.FromSeconds(5)
                                    };
                                    _imageTimer.Tick += ImageTimer_Tick;
                                }
                                // Ensure these UI operations are dispatched to the UI thread
                                await uiDispatcher.InvokeAsync(() => {
                                    UpdateImageAdsForLanguage();
                                    ShowNextImageAd();
                                });
                            }
                        }
                        else
                        {
                            Log("No image ads found in metadata, hiding image ad container", true);
                            if (_adContainer != null)
                            {
                                await uiDispatcher.InvokeAsync(() =>
                                {
                                    _adContainer.Visibility = Visibility.Collapsed;
                                    Log("Image ad container hidden", true);
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error processing image ads: {ex.Message}", true);
                        if (_adContainer != null)
                        {
                            await uiDispatcher.InvokeAsync(() =>
                            {
                                _adContainer.Visibility = Visibility.Collapsed;
                                Log("Image ad container hidden due to error", true);
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error loading image ads: {ex.Message}", true);
                    if (_adContainer != null)
                    {
                        await uiDispatcher.InvokeAsync(() =>
                        {
                            _adContainer.Visibility = Visibility.Collapsed;
                            Log("Image ad container hidden due to outer error", true);
                        });
                    }
                }
            });
        }

        private async Task PreloadImagesAsync(Dispatcher uiDispatcher)
        {
            if (_adLoader == null)
                return;

            foreach (var ad in _imageAds)
            {
                try
                {
                    if (_imageCache.ContainsKey(ad.File))
                        continue;

                    var imageData = await _adLoader.LoadImageFileAsync(ad.File);
                    if (imageData != null && imageData.Length > 0)
                    {
                        await uiDispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                var bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.StreamSource = new MemoryStream(imageData);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();
                                bitmap.Freeze();
                                _imageCache[ad.File] = bitmap;
                                Log($"Preloaded image: {ad.File}", true);
                            }
                            catch (Exception ex)
                            {
                                Log($"Error creating bitmap for {ad.File}: {ex.Message}", true);
                            }
                        });
                    }
                    else
                    {
                        Log($"Failed to load image data for {ad.File}", true);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error preloading image {ad.File}: {ex.Message}", true);
                }
            }
            Log($"Preloaded {_imageCache.Count} images", true);
        }

        private void UpdateImageAdsForLanguage()
        {
            // Ensure we're on the UI thread if we have a dispatcher
            if (_adContainer != null && _adContainer.Dispatcher != null && !_adContainer.Dispatcher.CheckAccess())
            {
                _adContainer.Dispatcher.Invoke(() => UpdateImageAdsForLanguage());
                return;
            }

            var filteredAds = _imageAds.Where(ad => ad.SupportsLanguage(_currentLanguage)).ToList();
            if (filteredAds.Count > 0)
            {
                Log($"Found {filteredAds.Count} image ads for language {_currentLanguage}", true);
                _imageAds = filteredAds;
                _currentImageAdIndex = 0;
            }
            else
            {
                Log($"No image ads found for language {_currentLanguage}, using all ads", true);
            }
        }

#if NET6_0_OR_GREATER
        private void ImageTimer_Tick(object? sender, EventArgs e)
#else
        private void ImageTimer_Tick(object sender, EventArgs e)
#endif
        {
            ShowNextImageAd();
        }

        private void ShowNextImageAd()
        {
            if (_adContainer == null || _imageAds.Count == 0 || _isTransitioning)
                return;

            // Ensure we're on the UI thread
            if (_adContainer.Dispatcher != null && !_adContainer.Dispatcher.CheckAccess())
            {
                _adContainer.Dispatcher.Invoke(() => ShowNextImageAd());
                return;
            }

            _isTransitioning = true;
            try
            {
                var nextAd = _imageAds[_currentImageAdIndex];
                _currentImageAdIndex = (_currentImageAdIndex + 1) % _imageAds.Count;
                if (_imageTimer != null)
                {
                    _imageTimer.Interval = TimeSpan.FromSeconds(nextAd.Duration);
                }

                var newImage = new Image
                {
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0
                };

                if (!string.IsNullOrEmpty(nextAd.Url))
                {
                    newImage.MouseDown += (s, e) =>
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = nextAd.Url,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            Log($"Error opening URL: {ex.Message}", true);
                        }
                    };
                    newImage.ToolTip = $"Click to open: {nextAd.Url}";
                }
                else if (!string.IsNullOrEmpty(nextAd.Description))
                {
                    newImage.ToolTip = nextAd.Description;
                }

                try
                {
#if NET6_0_OR_GREATER
                    if (_imageCache.TryGetValue(nextAd.File, out BitmapImage? cachedImage) && cachedImage != null)
#else
                    BitmapImage cachedImage;
                    if (_imageCache.TryGetValue(nextAd.File, out cachedImage) && cachedImage != null)
#endif
                    {
                        newImage.Source = cachedImage;
                        Log($"Using cached image for: {nextAd.File}", true);
                    }
                    else
                    {
                        Log($"Image not found in cache: {nextAd.File}", true);
                        _isTransitioning = false;
                        ShowNextImageAd();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error loading image {nextAd.File}: {ex.Message}", true);
                    _isTransitioning = false;
                    ShowNextImageAd();
                    return;
                }

                _adContainer.Children.Add(newImage);
                var fadeInAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromMilliseconds(500)
                };

                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(500)
                };

                fadeOutAnimation.Completed += (s, e) =>
                {
                    if (_currentImageControl != null && _adContainer.Children.Contains(_currentImageControl))
                    {
                        _adContainer.Children.Remove(_currentImageControl);
                    }
                    _currentImageControl = newImage;
                    _isTransitioning = false;
                    if (_imageTimer != null && !_imageTimer.IsEnabled)
                    {
                        _imageTimer.Start();
                    }
                };

                newImage.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);
                if (_currentImageControl != null)
                {
                    _currentImageControl.BeginAnimation(UIElement.OpacityProperty, fadeOutAnimation);
                }
                else
                {
                    _currentImageControl = newImage;
                    _isTransitioning = false;
                    if (_imageTimer != null && !_imageTimer.IsEnabled)
                    {
                        _imageTimer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error showing next image ad: {ex.Message}", true);
                _isTransitioning = false;
            }
        }
    }

    // Extension method to find visual children
    public static class VisualTreeExtensions
    {
        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    if (child != null)
                    {
                        foreach (T childOfChild in FindVisualChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }
    }
}