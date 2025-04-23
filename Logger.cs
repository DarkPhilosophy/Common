using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;
using System.Linq;

namespace Common
{
    /// <summary>
    /// Provides standardized logging functionality for applications.
    /// This class implements a consistent logging format with emoji indicators
    /// and can be used across different projects.
    /// </summary>
    public class Logger
    {
        // Singleton instance
#if NET6_0_OR_GREATER
        private static Logger? _instance;
#else
        private static Logger _instance;
#endif

        // Buffer to store log messages
        private readonly StringBuilder _logBuffer = new StringBuilder();

        // Maximum number of lines to keep in the buffer
        private const int MaxBufferLines = 100;

        // Delegate for log message callback
        public delegate void LogMessageCallback(string message, bool isError, bool isWarning, bool isSuccess, bool isInfo, bool consoleOnly);

        // Event for log message
#if NET6_0_OR_GREATER
        public event LogMessageCallback? OnLogMessage;
#else
        public event LogMessageCallback OnLogMessage;
#endif

        /// <summary>
        /// Gets the singleton instance of the Logger.
        /// </summary>
        public static Logger Instance
        {
            get
            {
#if NET6_0_OR_GREATER
                return _instance ??= new Logger();
#else
                if (_instance == null)
                {
                    _instance = new Logger();
                }
                return _instance;
#endif
            }
        }

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private Logger()
        {
        }

        /// <summary>
        /// Logs a message with optional type indicators.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="isError">Whether the message is an error.</param>
        /// <param name="isWarning">Whether the message is a warning.</param>
        /// <param name="isSuccess">Whether the message is a success message.</param>
        /// <param name="isInfo">Whether the message is an informational message.</param>
        /// <param name="consoleOnly">Whether to log only to the console and not to the UI.</param>
        public void LogMessage(string message, bool isError = false, bool isWarning = false, bool isSuccess = false, bool isInfo = false, bool consoleOnly = false)
        {
            // Create timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Format message with timestamp
            string formattedMessage = $"[{timestamp}] {message}";

            // Set emoji based on message type
            if (isError)
            {
                formattedMessage = $"❌ {formattedMessage}";
            }
            else if (isWarning)
            {
                formattedMessage = $"⚠️ {formattedMessage}";
            }
            else if (isSuccess)
            {
                formattedMessage = $"✅ {formattedMessage}";
            }
            else if (isInfo)
            {
                formattedMessage = $"🔍 {formattedMessage}";
            }
            else
            {
                formattedMessage = $"ℹ️ {formattedMessage}";
            }

            // Always write to console for debugging
            Console.WriteLine(formattedMessage);
            // Also write to debug output for troubleshooting
            System.Diagnostics.Debug.WriteLine("LOGGER: " + formattedMessage);

            // Only update log buffer if not console-only
            if (!consoleOnly)
            {
                // Add to log buffer - append to end to maintain chronological order
                // Ensure consistent newlines by adding a newline after each message
                _logBuffer.AppendLine(formattedMessage);

                // Limit buffer size to MaxBufferLines
                string bufferText = _logBuffer.ToString();
                string[] lines = bufferText.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > MaxBufferLines)
                {
                    _logBuffer.Clear();
                    // Keep the most recent MaxBufferLines lines
#if NET6_0_OR_GREATER
                    _logBuffer.Append(string.Join(Environment.NewLine, lines.Skip(lines.Length - MaxBufferLines)) + Environment.NewLine);
#else
                    // Manual implementation of Skip for .NET Framework 4.8
                    string[] recentLines = new string[MaxBufferLines];
                    Array.Copy(lines, lines.Length - MaxBufferLines, recentLines, 0, MaxBufferLines);
                    _logBuffer.Append(string.Join(Environment.NewLine, recentLines) + Environment.NewLine);
#endif
                }
            }

            // Trigger the event
            OnLogMessage?.Invoke(formattedMessage, isError, isWarning, isSuccess, isInfo, consoleOnly);
        }

        /// <summary>
        /// Gets the current log buffer content.
        /// </summary>
        /// <returns>The log buffer as a string.</returns>
        public string GetLogBuffer()
        {
            return _logBuffer.ToString();
        }

        /// <summary>
        /// Clears the log buffer.
        /// </summary>
        public void ClearLogBuffer()
        {
            _logBuffer.Clear();
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="consoleOnly">Whether to log only to the console.</param>
        public void LogError(string message, bool consoleOnly = false)
        {
            LogMessage(message, isError: true, consoleOnly: consoleOnly);
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="consoleOnly">Whether to log only to the console.</param>
        public void LogWarning(string message, bool consoleOnly = false)
        {
            LogMessage(message, isWarning: true, consoleOnly: consoleOnly);
        }

        /// <summary>
        /// Logs a success message.
        /// </summary>
        /// <param name="message">The success message to log.</param>
        /// <param name="consoleOnly">Whether to log only to the console.</param>
        public void LogSuccess(string message, bool consoleOnly = false)
        {
            LogMessage(message, isSuccess: true, consoleOnly: consoleOnly);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="message">The informational message to log.</param>
        /// <param name="consoleOnly">Whether to log only to the console.</param>
        public void LogInfo(string message, bool consoleOnly = false)
        {
            LogMessage(message, isInfo: true, consoleOnly: consoleOnly);
        }
    }
}
