using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using Common.Logging;

namespace Common.UI.Animation
{
    /// <summary>
    /// Provides centralized animation functionality for UI elements.
    /// This class implements various animations for buttons and other controls,
    /// including glowing effects, pulsing borders, and color transitions.
    /// Supports dynamic configuration, animation chaining, and gradient effects.
    /// </summary>
    public class AnimationManager
    {
        // Singleton instance
#if NET48
        private static AnimationManager _instance;
#else
        private static AnimationManager? _instance;
#endif

        // Dictionary to track active animations
        private readonly Dictionary<Button, List<AnimationState>> _activeAnimations = new Dictionary<Button, List<AnimationState>>();

        // Dictionary to store original button colors for red buttons
        private readonly Dictionary<Button, SolidColorBrush> _originalButtonColors = new Dictionary<Button, SolidColorBrush>();

        // Flag to enable/disable animations globally
        public bool AnimationsEnabled { get; set; } = true;

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
#if NET48
                if (_instance == null)
                {
                    _instance = new AnimationManager();
                }
                return _instance;
#else
                return _instance ??= new AnimationManager();
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
        /// Defines an animation's properties for flexible configuration.
        /// </summary>
        public class AnimationConfig
        {
            public AnimationType Type { get; set; } = AnimationType.Glow;
#if NET48
            public Brush TargetBrush { get; set; } = new SolidColorBrush(Colors.Blue); // Default value
#else
            public Brush? TargetBrush { get; set; } // SolidColorBrush or GradientBrush
#endif
            public double DurationSeconds { get; set; } = 0.8;
            public double From { get; set; } // Starting value (e.g., blur radius, scale)
            public double To { get; set; } // Ending value
            public double Intensity { get; set; } = 1.0; // Multiplier for effect strength
            public bool AutoReverse { get; set; } = true;
            public RepeatBehavior Repeat { get; set; } = RepeatBehavior.Forever;
            public IEasingFunction Easing { get; set; } = new SineEase { EasingMode = EasingMode.EaseInOut };
            public int Priority { get; set; } = 0; // Higher priority animations run first
        }

        /// <summary>
        /// Supported animation types
        /// </summary>
        public enum AnimationType
        {
            Glow, ScaleX, ScaleY, Rotate, Color, BorderThickness, Opacity
        }

        /// <summary>
        /// Tracks animation state for a button.
        /// </summary>
        private class AnimationState
        {
#if NET48
            public AnimationConfig Config { get; set; } = new AnimationConfig();
            public Storyboard Storyboard { get; set; } = new Storyboard();
#else
            public AnimationConfig? Config { get; set; }
            public Storyboard? Storyboard { get; set; }
#endif
            public bool IsRunning { get; set; }
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
        /// Creates a gradient brush for smooth color transitions.
        /// </summary>
        public static LinearGradientBrush CreateGradientBrush(Color start, Color end)
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1)
            };
            brush.GradientStops.Add(new GradientStop(start, 0));
            brush.GradientStops.Add(new GradientStop(end, 1));
            brush.Freeze();
            return brush;
        }

        /// <summary>
        /// Extracts a color from a brush (handles SolidColorBrush and LinearGradientBrush).
        /// </summary>
        private Color GetColorFromBrush(Brush brush)
        {
            if (brush is SolidColorBrush solid)
                return solid.Color;
            if (brush is LinearGradientBrush gradient && gradient.GradientStops.Count > 0)
                return gradient.GradientStops[0].Color;
            return Colors.Transparent;
        }

        /// <summary>
        /// Applies animations to a button based on configs.
        /// </summary>
        /// <param name="button">Target button.</param>
        /// <param name="configs">List of animation configurations.</param>
        public void ApplyAnimations(Button button, params AnimationConfig[] configs)
        {
            try
            {
                // Skip animations if globally disabled
                if (!AnimationsEnabled)
                {
                    // Just clean up any existing animations
                    StopAnimations(button);
                    return;
                }

                StopAnimations(button); // Clear existing animations

                var storyboard = new Storyboard();
                var states = new List<AnimationState>();

                // Sort configs by priority (higher priority first)
                var sortedConfigs = configs.OrderByDescending(c => c.Priority).ToArray();

                foreach (var config in sortedConfigs)
                {
                    var animation = CreateAnimation(button, config);
                    if (animation != null)
                    {
                        storyboard.Children.Add(animation);
                        states.Add(new AnimationState { Config = config, Storyboard = storyboard, IsRunning = true });
                    }
                }

                if (states.Count == 0) return;

                storyboard.Begin();
                _activeAnimations[button] = states;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error applying animations: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a single animation based on config.
        /// </summary>
        private Timeline CreateAnimation(Button button, AnimationConfig config)
        {
            switch (config.Type)
            {
                case AnimationType.Glow:
                    if (button.Effect == null)
                    {
                        button.Effect = new DropShadowEffect { Direction = 0, ShadowDepth = 0 };
                    }
                    var glow = (DropShadowEffect)button.Effect;
                    glow.Color = GetColorFromBrush(config.TargetBrush != null ? config.TargetBrush : new SolidColorBrush(Colors.Blue));
                    var blurAnim = new DoubleAnimation
                    {
                        From = config.From * config.Intensity,
                        To = config.To * config.Intensity,
                        Duration = TimeSpan.FromSeconds(config.DurationSeconds),
                        AutoReverse = config.AutoReverse,
                        RepeatBehavior = config.Repeat,
                        EasingFunction = config.Easing
                    };
                    Storyboard.SetTarget(blurAnim, button);
                    Storyboard.SetTargetProperty(blurAnim, new PropertyPath("(UIElement.Effect).(DropShadowEffect.BlurRadius)"));
                    return blurAnim;

                case AnimationType.ScaleX:
                case AnimationType.ScaleY:
                    EnsureScaleTransform(button);
                    var scaleProp = config.Type == AnimationType.ScaleX ? ScaleTransform.ScaleXProperty : ScaleTransform.ScaleYProperty;
                    var scaleAnim = new DoubleAnimation
                    {
                        From = config.From,
                        To = config.To,
                        Duration = TimeSpan.FromSeconds(config.DurationSeconds),
                        AutoReverse = config.AutoReverse,
                        RepeatBehavior = config.Repeat,
                        EasingFunction = config.Easing
                    };
                    Storyboard.SetTarget(scaleAnim, button);
                    Storyboard.SetTargetProperty(scaleAnim, new PropertyPath($"(UIElement.RenderTransform).(TransformGroup.Children)[0].{scaleProp.Name}"));
                    return scaleAnim;

                case AnimationType.Rotate:
                    EnsureRotateTransform(button);
                    var rotateAnim = new DoubleAnimation
                    {
                        From = config.From,
                        To = config.To,
                        Duration = TimeSpan.FromSeconds(config.DurationSeconds),
                        AutoReverse = config.AutoReverse,
                        RepeatBehavior = config.Repeat,
                        EasingFunction = config.Easing
                    };
                    Storyboard.SetTarget(rotateAnim, button);
                    Storyboard.SetTargetProperty(rotateAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(RotateTransform.Angle)"));
                    return rotateAnim;

                case AnimationType.Color:
                    var colorAnim = new ColorAnimation
                    {
                        From = GetColorFromBrush(button.Background),
                        To = GetColorFromBrush(config.TargetBrush != null ? config.TargetBrush : new SolidColorBrush(Colors.Blue)),
                        Duration = TimeSpan.FromSeconds(config.DurationSeconds),
                        AutoReverse = config.AutoReverse,
                        RepeatBehavior = config.Repeat,
                        EasingFunction = config.Easing
                    };
                    Storyboard.SetTarget(colorAnim, button);
                    Storyboard.SetTargetProperty(colorAnim, new PropertyPath("(Control.Background).(SolidColorBrush.Color)"));
                    return colorAnim;

                case AnimationType.BorderThickness:
                    var thicknessAnim = new ThicknessAnimation
                    {
                        From = new Thickness(config.From),
                        To = new Thickness(config.To),
                        Duration = TimeSpan.FromSeconds(config.DurationSeconds),
                        AutoReverse = config.AutoReverse,
                        RepeatBehavior = config.Repeat,
                        EasingFunction = config.Easing
                    };
                    Storyboard.SetTarget(thicknessAnim, button);
                    Storyboard.SetTargetProperty(thicknessAnim, new PropertyPath(Border.BorderThicknessProperty));
                    return thicknessAnim;

                case AnimationType.Opacity:
                    var opacityAnim = new DoubleAnimation
                    {
                        From = config.From,
                        To = config.To,
                        Duration = TimeSpan.FromSeconds(config.DurationSeconds),
                        AutoReverse = config.AutoReverse,
                        RepeatBehavior = config.Repeat,
                        EasingFunction = config.Easing
                    };
                    Storyboard.SetTarget(opacityAnim, button);
                    Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(UIElement.OpacityProperty));
                    return opacityAnim;

                default:
#if NET48
                    // Return a dummy animation that does nothing instead of null
                    var dummyAnim = new DoubleAnimation
                    {
                        Duration = TimeSpan.FromSeconds(0.1)
                    };
                    return dummyAnim;
#else
                    return null;
#endif
            }
        }

        /// <summary>
        /// Ensures a TransformGroup with ScaleTransform is set on the button.
        /// </summary>
        private void EnsureScaleTransform(Button button)
        {
            // Always set a fresh TransformGroup to avoid conflicts
            TransformGroup group = new TransformGroup();

            // Add a ScaleTransform
            ScaleTransform scaleTransform = new ScaleTransform(1.0, 1.0);
            group.Children.Add(scaleTransform);

            // Set the transform
            button.RenderTransform = group;
            button.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        /// <summary>
        /// Ensures a TransformGroup with RotateTransform is set on the button.
        /// </summary>
        private void EnsureRotateTransform(Button button)
        {
            // Always set a fresh TransformGroup to avoid conflicts
            TransformGroup group = new TransformGroup();

            // Add a RotateTransform
            RotateTransform rotateTransform = new RotateTransform(0);
            group.Children.Add(rotateTransform);

            // Set the transform
            button.RenderTransform = group;
            button.RenderTransformOrigin = new Point(0.5, 0.5);
        }

        /// <summary>
        /// Stops all animations on a button.
        /// </summary>
        public void StopAnimations(Button button)
        {
            if (_activeAnimations.TryGetValue(button, out var states))
            {
                foreach (var state in states)
                {
                    state.Storyboard?.Stop();
                    state.IsRunning = false;
                }
                _activeAnimations.Remove(button);

                // Clean up effects and transforms
                button.Effect = null;

                // Reset transform to identity
                button.RenderTransform = Transform.Identity;
                button.RenderTransformOrigin = new Point(0.5, 0.5);
            }
        }

        /// <summary>
        /// Applies a blue glow effect to a button.
        /// </summary>
        /// <param name="button">The button to apply the glow to.</param>
        public void ApplyBlueButtonGlow(Button button)
        {
            // First, ensure the button has the proper transform setup
            // This is crucial for animations to work correctly
            StopAnimations(button);

            // Reset the transform to ensure a clean state
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new ScaleTransform(1.0, 1.0));
            button.RenderTransform = transformGroup;
            button.RenderTransformOrigin = new Point(0.5, 0.5);

            // Use a much stronger, more vibrant blue color for better contrast against white
            Color strongBlue = Color.FromRgb(0x00, 0x66, 0xFF); // Stronger, more vibrant blue

            // Create animation configs for blue glow and scaling
            var configs = new[]
            {
                // Blue glow effect
                new AnimationConfig
                {
                    Type = AnimationType.Glow,
                    TargetBrush = new SolidColorBrush(strongBlue),
                    From = 30,
                    To = 55,
                    Intensity = 1.0,
                    DurationSeconds = 0.8,
                    AutoReverse = true,
                    Repeat = RepeatBehavior.Forever,
                    Priority = 3,
                    Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                },
                // Scale X animation
                new AnimationConfig
                {
                    Type = AnimationType.ScaleX,
                    From = 1.0,
                    To = 1.05,
                    DurationSeconds = 0.8,
                    AutoReverse = true,
                    Repeat = RepeatBehavior.Forever,
                    Priority = 1,
                    Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                },
                // Scale Y animation
                new AnimationConfig
                {
                    Type = AnimationType.ScaleY,
                    From = 1.0,
                    To = 1.05,
                    DurationSeconds = 0.8,
                    AutoReverse = true,
                    Repeat = RepeatBehavior.Forever,
                    Priority = 1,
                    Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                }
            };

            // Apply the animations
            ApplyAnimations(button, configs);
        }

        /// <summary>
        /// Applies a red glow effect to a button.
        /// </summary>
        /// <param name="button">The button to apply the glow to.</param>
        public void ApplyRedButtonGlow(Button button)
        {
            // Create animation configs for red glow
            var configs = new[]
            {
                new AnimationConfig
                {
                    Type = AnimationType.Glow,
                    TargetBrush = new SolidColorBrush(Colors.Red),
                    From = 15,
                    To = 25,
                    Intensity = 1.0,
                    DurationSeconds = 0.8
                }
            };

            // Apply the animations
            ApplyAnimations(button, configs);
        }

        /// <summary>
        /// Applies a custom color glow effect to a button with the specified tag.
        /// </summary>
        /// <param name="button">The button to apply the glow to.</param>
        /// <param name="color">The color of the glow.</param>
        /// <param name="intensity">The intensity of the glow (1 = normal, 2 = strong, 3 = very strong).</param>
        /// <param name="tagName">The tag name to set on the button (e.g., "GreenButton", "BlueButton").</param>
        public void ApplyCustomButtonGlow(Button button, Color color, int intensity, string tagName)
        {
            // Set the tag on the button
            button.Tag = tagName;

            // Define glow parameters based on intensity
            double blurRadius;
            double maxBlurRadius;
            double animationDuration;

            switch (intensity)
            {
                case 3: // Very strong glow
                    blurRadius = 30;
                    maxBlurRadius = 55;
                    animationDuration = 0.6;
                    break;
                case 2: // Strong glow
                    blurRadius = 20;
                    maxBlurRadius = 40;
                    animationDuration = 0.7;
                    break;
                case 1: // Normal glow
                default:
                    blurRadius = 15;
                    maxBlurRadius = 25;
                    animationDuration = 0.8;
                    break;
            }

            // Create animation configs
            var configs = new[]
            {
                new AnimationConfig
                {
                    Type = AnimationType.Glow,
                    TargetBrush = new SolidColorBrush(color),
                    From = blurRadius,
                    To = maxBlurRadius,
                    DurationSeconds = animationDuration
                }
            };

            // Apply the animations
            ApplyAnimations(button, configs);
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
                StopAnimations(button);

                // Apply the template first to ensure all template parts are created
                button.ApplyTemplate();

                // Find the border element in the button template
                if (button.Template.FindName("ButtonBorder", button) is Border border)
                {
                    // Create animation configs for template-based animation
                    var configs = new[]
                    {
                        // Blue glow effect
                        new AnimationConfig
                        {
                            Type = AnimationType.Glow,
                            TargetBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xFF)),
                            From = 30,
                            To = 55,
                            DurationSeconds = 0.6,
                            Priority = 2
                        },
                        // Scale X animation
                        new AnimationConfig
                        {
                            Type = AnimationType.ScaleX,
                            From = 1.0,
                            To = 1.03,
                            DurationSeconds = 0.6,
                            Priority = 1
                        },
                        // Scale Y animation
                        new AnimationConfig
                        {
                            Type = AnimationType.ScaleY,
                            From = 1.0,
                            To = 1.03,
                            DurationSeconds = 0.6,
                            Priority = 1
                        }
                    };

                    // Apply the animations
                    ApplyAnimations(button, configs);
                }
                else
                {
                    // Fall back to content-based animation
                    ContentBasedAnimation(button);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error starting animation: {ex.Message}");
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
                // Create animation configs for border animations
                var configs = new[]
                {
                    // Border thickness animation
                    new AnimationConfig
                    {
                        Type = AnimationType.BorderThickness,
                        From = 2,
                        To = 4,
                        DurationSeconds = 0.6,
                        Priority = 1
                    },
                    // Border color animation
                    new AnimationConfig
                    {
                        Type = AnimationType.Color,
                        TargetBrush = new SolidColorBrush(Colors.White),
                        DurationSeconds = 0.6,
                        Priority = 2
                    }
                };

                // Create a storyboard for the animations
                var storyboard = new Storyboard();
                var states = new List<AnimationState>();

                // Create border thickness animation
                var thicknessAnim = new ThicknessAnimation
                {
                    From = new Thickness(2),
                    To = new Thickness(4),
                    Duration = TimeSpan.FromSeconds(0.6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(thicknessAnim, border);
                Storyboard.SetTargetProperty(thicknessAnim, new PropertyPath(Border.BorderThicknessProperty));
                storyboard.Children.Add(thicknessAnim);

                // Create border color animation
                var colorAnim = new ColorAnimation
                {
                    From = Colors.Blue,
                    To = Colors.White,
                    Duration = TimeSpan.FromSeconds(0.6),
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Storyboard.SetTarget(colorAnim, border);
                Storyboard.SetTargetProperty(colorAnim, new PropertyPath("(Border.BorderBrush).(SolidColorBrush.Color)"));
                storyboard.Children.Add(colorAnim);

                // Start the animation
                storyboard.Begin();

                // Store the storyboard in our dictionary for later reference
                states.Add(new AnimationState
                {
                    Config = configs[0],
                    Storyboard = storyboard,
                    IsRunning = true
                });
                states.Add(new AnimationState
                {
                    Config = configs[1],
                    Storyboard = storyboard,
                    IsRunning = true
                });

                _activeAnimations[button] = states;
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

                // Stop all animations
                StopAnimations(button);

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
        /// Forcefully stops and restarts the pulsing animation on a button.
        /// This is used to restart the animation after mouse leave events.
        /// </summary>
        /// <param name="button">The button to restart animation on.</param>
        public void ForceRestartPulsingAnimation(Button button)
        {
            try
            {
                // Check if this is a button we should animate (blue or green)
                bool shouldAnimate = false;
                string buttonType = "";

                // Check for blue button
                if (button.Tag as string == "BlueButton")
                {
                    shouldAnimate = true;
                    buttonType = "BlueButton";
                }
                // Check for green button
                else if (button.Tag as string == "GreenButton")
                {
                    shouldAnimate = true;
                    buttonType = "GreenButton";
                }
                // Check by color if no tag is set
                else if (button.Background is SolidColorBrush brush)
                {
                    // Check if it's a blue button by color
                    if (brush.Color.R == BlueColor.Color.R &&
                        brush.Color.G == BlueColor.Color.G &&
                        brush.Color.B == BlueColor.Color.B)
                    {
                        shouldAnimate = true;
                        buttonType = "BlueButton";
                        // If it has the blue color but not the tag, set the tag
                        button.Tag = "BlueButton";
                        Logger.Instance.LogMessage("ForceRestartPulsingAnimation: Button has blue color but no BlueButton tag, setting tag", consoleOnly: true);
                    }
                    // Check if it's a green button by color
                    else if (brush.Color.R == GreenColor.Color.R &&
                             brush.Color.G == GreenColor.Color.G &&
                             brush.Color.B == GreenColor.Color.B)
                    {
                        shouldAnimate = true;
                        buttonType = "GreenButton";
                        // If it has the green color but not the tag, set the tag
                        button.Tag = "GreenButton";
                        Logger.Instance.LogMessage("ForceRestartPulsingAnimation: Button has green color but no GreenButton tag, setting tag", consoleOnly: true);
                    }
                }

                if (!shouldAnimate)
                {
                    Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Button is not a type we animate, Tag={button.Tag}", consoleOnly: true);
                    return;
                }

                Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Starting for button with Tag={button.Tag}", consoleOnly: true);

                // Stop any existing animations
                StopAnimations(button);

                // Create animation configs with enhanced effects
                var configs = new List<AnimationConfig>();

                // Add glow effect based on button type
                if (buttonType == "BlueButton")
                {
                    configs.Add(new AnimationConfig
                    {
                        Type = AnimationType.Glow,
                        TargetBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xFF)),
                        From = 30,
                        To = 55,
                        DurationSeconds = 0.6,
                        Priority = 3,
                        Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                    });
                }
                else if (buttonType == "GreenButton")
                {
                    configs.Add(new AnimationConfig
                    {
                        Type = AnimationType.Glow,
                        TargetBrush = new SolidColorBrush(GreenColor.Color),
                        From = 20,
                        To = 40,
                        DurationSeconds = 0.7,
                        Priority = 3,
                        Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                    });
                }

                // Add scale animations with enhanced values
                configs.Add(new AnimationConfig
                {
                    Type = AnimationType.ScaleX,
                    From = 1.0,
                    To = 1.05,
                    DurationSeconds = 0.8,
                    Priority = 2,
                    Easing = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                });

                configs.Add(new AnimationConfig
                {
                    Type = AnimationType.ScaleY,
                    From = 1.0,
                    To = 1.05,
                    DurationSeconds = 0.8,
                    Priority = 2,
                    Easing = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                });

                // Add subtle rotation for more dynamic effect
                configs.Add(new AnimationConfig
                {
                    Type = AnimationType.Rotate,
                    From = -0.5,
                    To = 0.5,
                    DurationSeconds = 1.2,
                    Priority = 1,
                    Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                });

                // Apply all animations
                ApplyAnimations(button, configs.ToArray());
                Logger.Instance.LogMessage($"ForceRestartPulsingAnimation: Applied {configs.Count} animations to button", consoleOnly: true);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error in ForceRestartPulsingAnimation: {ex.Message}");
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
                StopAnimations(button);

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

                // Create animation configs for red button corruption effect
                var configs = new[]
                {
                    // Glow effect
                    new AnimationConfig
                    {
                        Type = AnimationType.Glow,
                        TargetBrush = new SolidColorBrush(Colors.Red),
                        From = 5,
                        To = 20,
                        DurationSeconds = 0.5,
                        Priority = 3,
                        Easing = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 3 }
                    },
                    // Opacity pulsing
                    new AnimationConfig
                    {
                        Type = AnimationType.Opacity,
                        From = 0.9,
                        To = 1.0,
                        DurationSeconds = 0.5,
                        Priority = 2
                    },
                    // Subtle rotation
                    new AnimationConfig
                    {
                        Type = AnimationType.Rotate,
                        From = -1,
                        To = 1,
                        DurationSeconds = 0.3,
                        Priority = 1,
                        Easing = new BounceEase { EasingMode = EasingMode.EaseOut, Bounces = 2, Bounciness = 2 }
                    }
                };

                // Apply the animations
                ApplyAnimations(button, configs);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error in StartRedButtonCorruptionAnimation: {ex.Message}");
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
                // Stop all animations
                StopAnimations(button);

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
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error in StopRedButtonCorruptionAnimation: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops all active animations.
        /// </summary>
        public void StopAllAnimations()
        {
            try
            {
                foreach (var button in new List<Button>(_activeAnimations.Keys))
                {
                    StopAnimations(button);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error in StopAllAnimations: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a preset animation configuration for a specific effect.
        /// </summary>
        /// <param name="presetName">The name of the preset to create.</param>
        /// <returns>An array of animation configurations.</returns>
        public AnimationConfig[] CreateAnimationPreset(string presetName)
        {
            switch (presetName.ToLowerInvariant())
            {
                case "pulse":
                    return new[]
                    {
                        new AnimationConfig
                        {
                            Type = AnimationType.ScaleX,
                            From = 1.0,
                            To = 1.05,
                            DurationSeconds = 0.8,
                            Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                        },
                        new AnimationConfig
                        {
                            Type = AnimationType.ScaleY,
                            From = 1.0,
                            To = 1.05,
                            DurationSeconds = 0.8,
                            Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                        }
                    };

                case "heartbeat":
                    return new[]
                    {
                        new AnimationConfig
                        {
                            Type = AnimationType.ScaleX,
                            From = 1.0,
                            To = 1.08,
                            DurationSeconds = 0.4,
                            AutoReverse = true,
                            Repeat = new RepeatBehavior(2),
                            Easing = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                        },
                        new AnimationConfig
                        {
                            Type = AnimationType.ScaleY,
                            From = 1.0,
                            To = 1.08,
                            DurationSeconds = 0.4,
                            AutoReverse = true,
                            Repeat = new RepeatBehavior(2),
                            Easing = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                        }
                    };

                case "seductiveglow":
                    return new[]
                    {
                        new AnimationConfig
                        {
                            Type = AnimationType.Glow,
                            TargetBrush = CreateGradientBrush(Colors.Purple, Colors.DeepPink),
                            From = 10,
                            To = 25,
                            DurationSeconds = 1.2,
                            Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                        },
                        new AnimationConfig
                        {
                            Type = AnimationType.Rotate,
                            From = -1,
                            To = 1,
                            DurationSeconds = 2.0,
                            Easing = new SineEase { EasingMode = EasingMode.EaseInOut }
                        }
                    };

                default:
                    return new[] { new AnimationConfig() };
            }
        }
    }
}
