using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Common
{
    /// <summary>
    /// Provides centralized animation functionality for UI elements.
    /// This class implements various animations for buttons and other controls,
    /// including glowing effects, pulsing borders, and color transitions.
    /// </summary>
    public class AnimationManager
    {
        // Singleton instance
#if NET6_0_OR_GREATER
        private static AnimationManager? _instance;
#else
        private static AnimationManager _instance;
#endif

        // Dictionary to track active animations
        public readonly Dictionary<Button, object> _activeAnimations = new Dictionary<Button, object>();

        // Dictionary to store original button colors for red buttons
        private readonly Dictionary<Button, SolidColorBrush> _originalButtonColors = new Dictionary<Button, SolidColorBrush>();

        // Button colors - create with identical properties to ensure consistent sizing
        public static readonly SolidColorBrush BlueColor = CreateButtonBrush(0x21, 0x96, 0xF3); // #2196F3
        public static readonly SolidColorBrush RedColor = CreateButtonBrush(0xF4, 0x43, 0x36); // #F44336
        public static readonly SolidColorBrush GreenColor = CreateButtonBrush(0x4C, 0xAF, 0x50); // #4CAF50
        public static readonly SolidColorBrush YellowColor = CreateButtonBrush(0xFF, 0xC1, 0x07); // #FFC107
        public static readonly SolidColorBrush GrayColor = CreateButtonBrush(0x9E, 0x9E, 0x9E); // #9E9E9E

        /// <summary>
        /// Gets the singleton instance of the AnimationManager.
        /// </summary>
        public static AnimationManager Instance
        {
            get
            {
#if NET6_0_OR_GREATER
                return _instance ??= new AnimationManager();
#else
                if (_instance == null)
                {
                    _instance = new AnimationManager();
                }
                return _instance;
#endif
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private AnimationManager()
        {
        }

        /// <summary>
        /// Helper method to create button brushes with consistent properties.
        /// </summary>
        private static SolidColorBrush CreateButtonBrush(byte r, byte g, byte b)
        {
            // Create a new color with full alpha channel
            Color color = Color.FromArgb(255, r, g, b);

            // Create a new brush with the color
            var brush = new SolidColorBrush(color);

            // Set explicit properties to ensure consistency
            brush.Opacity = 1.0;
            brush.Transform = null;
            brush.RelativeTransform = null;

            // Freeze the brush to improve performance and prevent modification
            brush.Freeze();

            return brush;
        }

        /// <summary>
        /// Applies a blue glow effect to a button.
        /// </summary>
        /// <param name="button">The button to apply the glow to.</param>
        public void ApplyBlueButtonGlow(Button button)
        {
            // Use a much stronger, more vibrant blue color for better contrast against white
            Color strongBlue = Color.FromRgb(0x00, 0x66, 0xFF); // Stronger, more vibrant blue

            // Apply a very strong blue glow (intensity level 3)
            ApplyButtonGlow(button, strongBlue, 3);
        }

        /// <summary>
        /// Applies a red glow effect to a button.
        /// </summary>
        /// <param name="button">The button to apply the glow to.</param>
        public void ApplyRedButtonGlow(Button button)
        {
            // Apply a less intense red glow (intensity level 1)
            ApplyButtonGlow(button, Colors.Red, 1);
        }

        /// <summary>
        /// Applies a standardized glow effect to a button with the specified color and intensity.
        /// </summary>
        /// <param name="button">The button to apply the glow to.</param>
        /// <param name="color">The color of the glow.</param>
        /// <param name="intensity">The intensity of the glow (1 = normal, 2 = strong, 3 = very strong).</param>
        public void ApplyButtonGlow(Button button, Color color, int intensity)
        {
            try
            {
                // Define glow parameters based on intensity
                double blurRadius;
                double maxBlurRadius;
                double opacity;
                double maxOpacity;
                double animationDuration;

                switch (intensity)
                {
                    case 3: // Very strong glow (for blue buttons)
                        blurRadius = 30;
                        maxBlurRadius = 55;
                        opacity = 0.9;
                        maxOpacity = 1.0;
                        animationDuration = 0.6;
                        break;
                    case 2: // Strong glow
                        blurRadius = 20;
                        maxBlurRadius = 40;
                        opacity = 0.8;
                        maxOpacity = 0.9;
                        animationDuration = 0.7;
                        break;
                    case 1: // Normal glow (for red buttons)
                    default:
                        blurRadius = 15;
                        maxBlurRadius = 25;
                        opacity = 0.7;
                        maxOpacity = 0.8;
                        animationDuration = 0.8;
                        break;
                }

                // Add the glow effect
                button.Effect = new DropShadowEffect
                {
                    Color = color,
                    Direction = 0,
                    ShadowDepth = 0,
                    BlurRadius = blurRadius,
                    Opacity = opacity
                };

                // Add a glow animation for a pulsing effect
                var storyboard = new Storyboard();

                var blurRadiusAnimation = new DoubleAnimation
                {
                    From = blurRadius,
                    To = maxBlurRadius,
                    Duration = TimeSpan.FromSeconds(animationDuration),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                Storyboard.SetTarget(blurRadiusAnimation, button);
                Storyboard.SetTargetProperty(blurRadiusAnimation, new PropertyPath("(UIElement.Effect).(DropShadowEffect.BlurRadius)"));
                storyboard.Children.Add(blurRadiusAnimation);

                // Add a glow opacity animation
                var glowOpacityAnimation = new DoubleAnimation
                {
                    From = opacity,
                    To = maxOpacity,
                    Duration = TimeSpan.FromSeconds(animationDuration),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                Storyboard.SetTarget(glowOpacityAnimation, button);
                Storyboard.SetTargetProperty(glowOpacityAnimation, new PropertyPath("(UIElement.Effect).(DropShadowEffect.Opacity)"));
                storyboard.Children.Add(glowOpacityAnimation);

                // Start the animation
                storyboard.Begin();

                // Store the storyboard in our dictionary for later reference
                _activeAnimations[button] = storyboard;
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the application
                Logger.Instance.LogError($"Error applying button glow: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts a pulsing border animation on a button.
        /// </summary>
        /// <param name="button">The button to animate.</param>
        /// <param name="resourceDictionary">The resource dictionary containing animation resources.</param>
#if NET6_0_OR_GREATER
        public void StartPulsingBorderAnimation(Button button, ResourceDictionary? resourceDictionary = null)
#else
        public void StartPulsingBorderAnimation(Button button, ResourceDictionary resourceDictionary = null)
#endif
        {
            try
            {
                // Stop any existing animation on this button
                StopPulsingBorderAnimation(button);

                // Try to use template-based animation first (ConfigReplacer style)
                if (TryTemplateBasedAnimation(button))
                {
                    return;
                }

                // Fall back to content-based animation (CSVGenerator style)
                ContentBasedAnimation(button);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error starting animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Attempts to apply a template-based animation to the button.
        /// </summary>
        /// <param name="button">The button to animate.</param>
        /// <returns>True if successful, false otherwise.</returns>
        private bool TryTemplateBasedAnimation(Button button)
        {
            try
            {
                // Apply the template first to ensure all template parts are created
                button.ApplyTemplate();

                // Find the border element in the button template
                if (button.Template.FindName("ButtonBorder", button) is Border border)
                {
                    // Apply a blue glow effect directly
                    ApplyBlueButtonGlow(button);

                    // Create a simple storyboard for scale animation
                    var storyboard = new Storyboard();

                    // Create scale animations
                    var scaleXAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.03,
                        Duration = TimeSpan.FromSeconds(0.6),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };

                    var scaleYAnimation = new DoubleAnimation
                    {
                        From = 1.0,
                        To = 1.03,
                        Duration = TimeSpan.FromSeconds(0.6),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };

                    // Ensure the button has a ScaleTransform
#if NET6_0_OR_GREATER
                    if (button.RenderTransform is not ScaleTransform)
#else
                    if (!(button.RenderTransform is ScaleTransform))
#endif
                    {
                        button.RenderTransform = new ScaleTransform(1.0, 1.0);
                        button.RenderTransformOrigin = new Point(0.5, 0.5);
                    }

                    // Set targets for scale animations
                    Storyboard.SetTarget(scaleXAnimation, button);
                    Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                    storyboard.Children.Add(scaleXAnimation);

                    Storyboard.SetTarget(scaleYAnimation, button);
                    Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                    storyboard.Children.Add(scaleYAnimation);

                    // Start the animation
                    storyboard.Begin();

                    // Store the storyboard in our dictionary for later reference
                    _activeAnimations[button] = storyboard;

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Applies a content-based animation to the button.
        /// </summary>
        /// <param name="button">The button to animate.</param>
        private void ContentBasedAnimation(Button button)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Colors.Blue),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(-2)
            };

            var originalContent = button.Content;
            var contentPresenter = new ContentPresenter { Content = originalContent };
            border.Child = contentPresenter;
            button.Content = border;

            try
            {
                var storyboard = new Storyboard();

                var thicknessAnimation = new ThicknessAnimation
                {
                    From = new Thickness(2),
                    To = new Thickness(4),
                    Duration = TimeSpan.FromSeconds(0.6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(thicknessAnimation, border);
                Storyboard.SetTargetProperty(thicknessAnimation, new PropertyPath(Border.BorderThicknessProperty));
                storyboard.Children.Add(thicknessAnimation);

                var colorAnimation = new ColorAnimation
                {
                    From = Colors.Blue,
                    To = Colors.White,
                    Duration = TimeSpan.FromSeconds(0.6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(colorAnimation, border);
                Storyboard.SetTargetProperty(colorAnimation, new PropertyPath("(Border.BorderBrush).(SolidColorBrush.Color)"));
                storyboard.Children.Add(colorAnimation);

                storyboard.Begin();
                _activeAnimations[button] = storyboard;
            }
            catch (Exception ex)
            {
                border.BorderThickness = new Thickness(2);
                Logger.Instance.LogError($"Error applying animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops a pulsing border animation on a button.
        /// </summary>
        /// <param name="button">The button to stop animating.</param>
        public void StopPulsingBorderAnimation(Button button)
        {
            try
            {
                // Skip if this is a red button that's currently being hovered
                if (_originalButtonColors.ContainsKey(button))
                {
                    return;
                }

                // Skip if this is a blue button - we want to keep the blue glow
                if (button.Tag as string == "BlueButton" ||
                    (button.Background is SolidColorBrush brush && brush.Color == BlueColor.Color))
                {
                    return;
                }

                // Check if we have an active animation for this button
                if (_activeAnimations.TryGetValue(button, out var animation))
                {
                    try
                    {
                        // Handle different animation types
                        if (animation is Storyboard storyboard)
                        {
                            storyboard.Stop();
                        }
                        else if (animation is DispatcherTimer timer)
                        {
                            timer.Stop();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"?? [DEBUG] Error stopping animation: {ex.Message}");
                    }

                    // Remove from our dictionary
                    _activeAnimations.Remove(button);
                }

                // Reset border thickness and remove all glow effects
                button.ApplyTemplate();
                if (button.Template.FindName("ButtonBorder", button) is Border border)
                {
                    border.BorderThickness = new Thickness(0);
                    border.Effect = null;
                }

                // Also remove the glow effect from the button itself
                button.Effect = null;

                // Check if we need to restore the original content (for content-based animation)
                if (button.Content is Border contentBorder && contentBorder.Child is ContentPresenter contentPresenter)
                {
                    button.Content = contentPresenter.Content;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error stopping animation: {ex.Message}");
            }
        }

        /// <summary>
        /// Forcefully stops and restarts the pulsing animation on a blue button.
        /// This is used to restart the animation after mouse leave events.
        /// </summary>
        /// <param name="button">The button to restart animation on.</param>
        public void ForceRestartPulsingAnimation(Button button)
        {
            try
            {
                // Only proceed if this is a blue button
                bool isBlueButton = false;

                if (button.Tag as string == "BlueButton")
                {
                    isBlueButton = true;
                }
                else if (button.Background is SolidColorBrush brush &&
                         brush.Color.R == BlueColor.Color.R &&
                         brush.Color.G == BlueColor.Color.G &&
                         brush.Color.B == BlueColor.Color.B)
                {
                    isBlueButton = true;
                    // If it has the blue color but not the tag, set the tag
                    button.Tag = "BlueButton";
                    Console.WriteLine($"?? [DEBUG] ForceRestartPulsingAnimation: Button has blue color but no BlueButton tag, setting tag");
                }

                if (!isBlueButton)
                {
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Button is not blue, Tag={button.Tag}", consoleOnly: true);
                    return;
                }

                Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Starting for button with Tag={button.Tag}", consoleOnly: true);

                // First stop any existing animation
                if (_activeAnimations.TryGetValue(button, out var animation))
                {
                    try
                    {
                        // Handle different animation types
                        if (animation is Storyboard storyboard)
                        {
                            storyboard.Stop();
                        }
                        else if (animation is DispatcherTimer timer)
                        {
                            timer.Stop();
                        }
                        _activeAnimations.Remove(button);
                        Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Stopped existing animation", consoleOnly: true);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Error stopping animation: {ex.Message}", isError: true, consoleOnly: true);
                    }
                }

                // Reset the transform to ensure we start from a clean state
                if (button.RenderTransform is ScaleTransform scaleTransform)
                {
                    scaleTransform.ScaleX = 1.0;
                    scaleTransform.ScaleY = 1.0;
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Reset existing ScaleTransform", consoleOnly: true);
                }
                else
                {
                    button.RenderTransform = new ScaleTransform(1.0, 1.0);
                    button.RenderTransformOrigin = new Point(0.5, 0.5);
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Created new ScaleTransform", consoleOnly: true);
                }

                // Create a new storyboard for the size pulsing animation
                var newStoryboard = new Storyboard();

                // Create scale animations with larger values for more noticeable effect
                var scaleXAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.05,  // Increased from 1.03 to 1.05
                    Duration = TimeSpan.FromSeconds(0.8),  // Slowed down from 0.6 to 0.8
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                var scaleYAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 1.05,  // Increased from 1.03 to 1.05
                    Duration = TimeSpan.FromSeconds(0.8),  // Slowed down from 0.6 to 0.8
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                // Set targets for scale animations
                Storyboard.SetTarget(scaleXAnimation, button);
                Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
                newStoryboard.Children.Add(scaleXAnimation);

                Storyboard.SetTarget(scaleYAnimation, button);
                Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
                newStoryboard.Children.Add(scaleYAnimation);

                // Use a dedicated NameScope for this animation to avoid conflicts with other animations
                NameScope.SetNameScope(button, new NameScope());
                Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Set new NameScope for button", consoleOnly: true);

                // Register the transform with the NameScope
                if (button.RenderTransform is ScaleTransform transformObj)
                {
                    string transformName = $"ScaleTransform_{button.GetHashCode()}";
                    var scope = NameScope.GetNameScope(button);
                    scope?.RegisterName(transformName, transformObj);
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Registered transform with name {transformName}, scope is null: {scope == null}", consoleOnly: true);
                }
                else
                {
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Button RenderTransform is not a ScaleTransform: {button.RenderTransform?.GetType().Name ?? "null"}", isWarning: true, consoleOnly: true);
                }

                // Start the animation using a storyboard for better compatibility
                try
                {
                    // Start the storyboard animation
                    newStoryboard.Begin();
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Started storyboard animation", consoleOnly: true);

                    // Store the storyboard in our dictionary for later reference
                    _activeAnimations[button] = newStoryboard;
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Stored storyboard in _activeAnimations dictionary", consoleOnly: true);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Error starting storyboard: {ex.Message}", isError: true, consoleOnly: true);

                    // If storyboard approach fails, try direct animation
                    try
                    {
                        // Create direct animations for the transform
                        if (button.RenderTransform is ScaleTransform directTransform)
                        {
                            var xAnim = new DoubleAnimation
                            {
                                From = 1.0,
                                To = 1.05,
                                Duration = TimeSpan.FromSeconds(0.8),
                                AutoReverse = true,
                                RepeatBehavior = RepeatBehavior.Forever
                            };

                            var yAnim = new DoubleAnimation
                            {
                                From = 1.0,
                                To = 1.05,
                                Duration = TimeSpan.FromSeconds(0.8),
                                AutoReverse = true,
                                RepeatBehavior = RepeatBehavior.Forever
                            };

                            // Apply the animations directly to the transform properties
                            directTransform.BeginAnimation(ScaleTransform.ScaleXProperty, xAnim);
                            directTransform.BeginAnimation(ScaleTransform.ScaleYProperty, yAnim);

                            Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Started direct transform animation", consoleOnly: true);

                            // Create a dummy storyboard to store in the dictionary
                            var dummyStoryboard = new Storyboard();
                            _activeAnimations[button] = dummyStoryboard;
                        }
                    }
                    catch (Exception ex2)
                    {
                        Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Error with direct animation: {ex2.Message}", isError: true, consoleOnly: true);
                        // If all animation attempts fail, just set a fixed scale
                        if (button.RenderTransform is ScaleTransform transform2)
                        {
                            transform2.ScaleX = 1.03;
                            transform2.ScaleY = 1.03;
                        }
                    }
                }

                // Make sure the blue glow is still applied
                ApplyBlueButtonGlow(button);
            }
            catch (Exception)
            {
                // Silently handle the error
            }
        }

        /// <summary>
        /// Handles the MouseEnter event for red buttons.
        /// </summary>
        /// <param name="button">The button that was entered.</param>
        public void HandleRedButtonMouseEnter(Button button)
        {
            if (button.Background == RedColor)
            {
                // Store the original background color
                _originalButtonColors[button] = RedColor;

                // Change to gray and disable the button
                button.Background = GrayColor;
                button.IsEnabled = false;
            }
        }

        /// <summary>
        /// Handles the MouseLeave event for red buttons.
        /// </summary>
        /// <param name="button">The button that was left.</param>
        /// <param name="updateCallback">Callback to update button colors after leaving.</param>
        public void HandleRedButtonMouseLeave(Button button, Action updateCallback)
        {
            if (_originalButtonColors.TryGetValue(button, out var originalColor))
            {
                // Restore the original background color
                button.Background = originalColor;
                button.IsEnabled = true;

                // Remove from dictionary
                _originalButtonColors.Remove(button);

                // Update button colors to ensure proper state
                updateCallback?.Invoke();
            }
        }

        /// <summary>
        /// Starts a "corrupted" animation for red buttons, with black pulsing effect.
        /// </summary>
        /// <param name="button">The button to animate.</param>
        public void StartRedButtonCorruptionAnimation(Button button)
        {
            try
            {
                // First stop any existing animation
                StopRedButtonCorruptionAnimation(button);

                // Apply the template first to ensure all template parts are created
                button.ApplyTemplate();

                // Create a special cursor effect for red buttons
                // This will show a "not allowed" cursor when hovering over red buttons
                button.Cursor = Cursors.No;

                // Store the original background for restoration later
                if (button.Background is SolidColorBrush originalBrush)
                {
                    // Store the original color in the button's data context for restoration
                    button.DataContext = originalBrush.Clone();
                }

                // Change the background to gray when hovering over a red button
                button.Background = GrayColor;

                // Add a strong glow effect to make it more dramatic
                button.Effect = new DropShadowEffect
                {
                    Color = Colors.Red,
                    Direction = 0,
                    ShadowDepth = 0,
                    BlurRadius = 20,
                    Opacity = 1.0
                };

                // Create a storyboard for the animations
                var storyboard = new Storyboard();

                // Add a glow animation for the shadow effect
                var glowAnimation = new DoubleAnimation
                {
                    From = 5,
                    To = 20,
                    Duration = TimeSpan.FromSeconds(0.5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                Storyboard.SetTarget(glowAnimation, button);
                Storyboard.SetTargetProperty(glowAnimation, new PropertyPath("(UIElement.Effect).(DropShadowEffect.BlurRadius)"));
                storyboard.Children.Add(glowAnimation);

                // Add a glow opacity animation
                var glowOpacityAnimation = new DoubleAnimation
                {
                    From = 0.8,
                    To = 1.0,
                    Duration = TimeSpan.FromSeconds(0.5),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };

                Storyboard.SetTarget(glowOpacityAnimation, button);
                Storyboard.SetTargetProperty(glowOpacityAnimation, new PropertyPath("(UIElement.Effect).(DropShadowEffect.Opacity)"));
                storyboard.Children.Add(glowOpacityAnimation);

                // Start the animation
                storyboard.Begin();

                // Store the storyboard in our dictionary for later reference
                _activeAnimations[button] = storyboard;
            }
            catch (Exception)
            {
                // Silently handle the error
            }
        }

        /// <summary>
        /// Stops the corruption animation on a red button.
        /// </summary>
        /// <param name="button">The button to stop animating.</param>
        public void StopRedButtonCorruptionAnimation(Button button)
        {
            try
            {
                // Check if we have an active animation for this button
                if (_activeAnimations.TryGetValue(button, out var animation))
                {
                    try
                    {
                        // Handle different animation types
                        if (animation is Storyboard storyboard)
                        {
                            storyboard.Stop();
                        }
                        else if (animation is DispatcherTimer timer)
                        {
                            timer.Stop();
                        }
                    }
                    catch (Exception)
                    {
                        // Silently handle the error
                    }

                    // Remove from our dictionary
                    _activeAnimations.Remove(button);
                }

                // Reset the cursor
                button.Cursor = null;

                // Check if the button is still red or blue - if so, reapply the appropriate glow effect
                bool isStillRed = false;
                bool isStillBlue = false;

                if (button.Background is SolidColorBrush brush)
                {
                    if (brush.Color == RedColor.Color)
                    {
                        isStillRed = true;
                    }
                    else if (brush.Color == BlueColor.Color)
                    {
                        isStillBlue = true;
                    }
                }

                if (button.Tag as string == "RedButton")
                {
                    isStillRed = true;
                }
                else if (button.Tag as string == "BlueButton")
                {
                    isStillBlue = true;
                }

                // Restore the original background if we saved it
                if (button.DataContext is SolidColorBrush originalBrush)
                {
                    button.Background = originalBrush;
                }

                if (isStillRed)
                {
                    // Reapply the red glow effect since the button is still red
                    ApplyRedButtonGlow(button);
                }
                else if (isStillBlue)
                {
                    // Reapply the blue glow effect since the button is still blue
                    ApplyBlueButtonGlow(button);
                }
                else
                {
                    // Remove the glow effect if the button is neither red nor blue
                    button.Effect = null;
                }
            }
            catch (Exception)
            {
                // Silently handle the error
            }
        }

        /// <summary>
        /// Stops all active animations.
        /// </summary>
        public void StopAllAnimations()
        {
            foreach (var button in new List<Button>(_activeAnimations.Keys))
            {
                StopPulsingBorderAnimation(button);
                StopRedButtonCorruptionAnimation(button);
            }
        }
    }
}
