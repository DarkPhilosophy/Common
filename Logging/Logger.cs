using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Common.Logging
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
        public delegate void LogMessageCallback(string message, bool isError, bool isWarning, bool isSuccess, bool isInfo, bool consoleOnly, Dictionary<string, object> structuredData);

        // Event for log message
#if NET6_0_OR_GREATER
        public event LogMessageCallback? OnLogMessage;
#else
        public event LogMessageCallback OnLogMessage;
#endif

        // Dictionary to store error codes and their descriptions
        private static readonly Dictionary<string, string> ErrorCodes = new Dictionary<string, string>()
        {
            // Configuration related errors (CFG)
            {"CFG-001", "File not found: The specified configuration file could not be found."},
            {"CFG-002", "Invalid JSON: The configuration file contains invalid JSON syntax."},
            {"CFG-003", "Access denied: You don't have permission to access the configuration file."},
            {"CFG-004", "Directory creation failed: Unable to create the directory for the configuration file."},
            {"CFG-005", "File read error: Unable to read the configuration file."},
            {"CFG-006", "File write error: Unable to write to the configuration file."},
            {"CFG-007", "Missing required setting: A required configuration setting is missing."},
            {"CFG-008", "Invalid setting value: A configuration setting has an invalid value."},

            // CSV related errors (CSV)
            {"CSV-001", "Invalid file format: The CSV file has an invalid format."},
            {"CSV-002", "Missing data: Required data is missing from the CSV file."},
            {"CSV-003", "Unit conversion error: Failed to convert between different units."},
            {"CSV-004", "File parsing error: Error occurred while parsing the CSV file."},
            {"CSV-005", "Invalid numeric value: A numeric value in the CSV file is invalid."},
            {"CSV-006", "Duplicate entry: A duplicate entry was found in the CSV file."},
            {"CSV-007", "File generation error: Error occurred while generating the CSV file."},
            {"CSV-008", "Empty file: The CSV file is empty."},

            // Application related errors (APP)
            {"APP-001", "Initialization error: Failed to initialize the application."},
            {"APP-002", "Resource loading error: Failed to load a required resource."},
            {"APP-003", "UI error: An error occurred in the user interface."},
            {"APP-004", "Operation cancelled: The operation was cancelled by the user."},
            {"APP-005", "Timeout: The operation timed out."},
            {"APP-006", "Unexpected error: An unexpected error occurred."},

            // File system related errors (FS)
            {"FS-001", "File not found: The specified file could not be found."},
            {"FS-002", "Directory not found: The specified directory could not be found."},
            {"FS-003", "Access denied: You don't have permission to access the file or directory."},
            {"FS-004", "File in use: The file is being used by another process."},
            {"FS-005", "Disk full: There is not enough disk space."},
            {"FS-006", "Path too long: The specified path is too long."},
            {"FS-007", "Invalid path: The specified path is invalid."},

            // Network related errors (NET)
            {"NET-001", "Connection failed: Failed to establish a network connection."},
            {"NET-002", "Timeout: The network operation timed out."},
            {"NET-003", "Resource not found: The requested network resource could not be found."},
            {"NET-004", "Authentication failed: Failed to authenticate with the network resource."}
        };

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
        /// Logs a message with optional type indicators and structured data.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="isError">Whether the message is an error.</param>
        /// <param name="isWarning">Whether the message is a warning.</param>
        /// <param name="isSuccess">Whether the message is a success message.</param>
        /// <param name="isInfo">Whether the message is an informational message.</param>
        /// <param name="consoleOnly">Whether to log only to the console and not to the UI.</param>
        /// <param name="structuredData">A dictionary containing structured data to be logged.</param>
#if NET6_0_OR_GREATER
        public void LogMessage(string message, bool isError = false, bool isWarning = false, bool isSuccess = false, bool isInfo = false, bool consoleOnly = false, Dictionary<string, object>? structuredData = null)
#else
        public void LogMessage(string message, bool isError = false, bool isWarning = false, bool isSuccess = false, bool isInfo = false, bool consoleOnly = false, Dictionary<string, object> structuredData = null)
#endif
        {
            // Create timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss");

            // Initialize structured data if null
            var data = structuredData ?? new Dictionary<string, object>();

            // Add timestamp to structured data
            data["Timestamp"] = DateTime.Now;

            // Add message type to structured data
            string messageType = "Info";
            if (isError) messageType = "Error";
            else if (isWarning) messageType = "Warning";
            else if (isSuccess) messageType = "Success";
            else if (isInfo) messageType = "Info";
            data["MessageType"] = messageType;

            // Determine error code if it's an error
            string errorCode = string.Empty;
            if (isError && data.ContainsKey("ErrorCode") && data["ErrorCode"] is string)
            {
                errorCode = (string)data["ErrorCode"];
                if (!ErrorCodes.ContainsKey(errorCode))
                {
                    // Log warning about invalid error code but still use it
                    System.Diagnostics.Debug.WriteLine($"WARNING: Unknown error code used: {errorCode}");
                }
            }

            // Add thread ID to structured data
            data["ThreadId"] = System.Threading.Thread.CurrentThread.ManagedThreadId;

            // Format message with timestamp and error code
            string formattedMessage = $"[{timestamp}] ";
            if (!string.IsNullOrEmpty(errorCode))
            {
                formattedMessage += $"[{errorCode}] ";

                // Add user-friendly error description if available
                string errorDescription = string.Empty;
                if (ErrorCodes.TryGetValue(errorCode, out errorDescription))
                {
                    // Extract the short description (before the colon)
                    int colonIndex = errorDescription.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        data["ErrorDescription"] = errorDescription.Substring(0, colonIndex).Trim();
                    }
                    else
                    {
                        data["ErrorDescription"] = errorDescription;
                    }
                }
            }
            formattedMessage += message;

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

            // Add suggestions for errors and warnings
            if ((isError || isWarning) && !string.IsNullOrEmpty(errorCode))
            {
                // Add suggestion based on error code prefix
                string suggestion = string.Empty;

                if (errorCode.StartsWith("CFG-"))
                {
                    suggestion = "Try checking your configuration settings or reinstalling the application.";
                }
                else if (errorCode.StartsWith("CSV-"))
                {
                    suggestion = "Try checking the CSV file format or recreating the file.";
                }
                else if (errorCode.StartsWith("FS-"))
                {
                    suggestion = "Try checking file permissions or disk space.";
                }
                else if (errorCode.StartsWith("NET-"))
                {
                    suggestion = "Try checking your network connection or firewall settings.";
                }
                else if (errorCode.StartsWith("APP-"))
                {
                    suggestion = "Try restarting the application.";
                }

                if (!string.IsNullOrEmpty(suggestion))
                {
                    data["Suggestion"] = suggestion;

                    // Only add suggestion to console output for errors
                    if (isError)
                    {
                        formattedMessage += $" {suggestion}";
                    }
                }
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
            OnLogMessage?.Invoke(formattedMessage, isError, isWarning, isSuccess, isInfo, consoleOnly, data);
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
        /// <param name="errorCode">The error code associated with the error.</param>
        /// <param name="structuredData">A dictionary containing structured data to be logged.</param>
#if NET6_0_OR_GREATER
        public void LogError(string message, bool consoleOnly = false, string errorCode = "", Dictionary<string, object>? structuredData = null)
#else
        public void LogError(string message, bool consoleOnly = false, string errorCode = "", Dictionary<string, object> structuredData = null)
#endif
        {
            var data = structuredData ?? new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(errorCode))
            {
                data["ErrorCode"] = errorCode;
            }
            LogMessage(message, isError: true, consoleOnly: consoleOnly, structuredData: data);
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

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="consoleOnly">Whether to log only to the console.</param>
        /// <param name="structuredData">A dictionary containing structured data to be logged.</param>
#if NET6_0_OR_GREATER
        public void LogWarning(string message, bool consoleOnly = false, Dictionary<string, object>? structuredData = null)
#else
        public void LogWarning(string message, bool consoleOnly = false, Dictionary<string, object> structuredData = null)
#endif
        {
            LogMessage(message, isWarning: true, consoleOnly: consoleOnly, structuredData: structuredData);
        }

        /// <summary>
        /// Logs an exception with detailed information.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="context">Additional context information about where the exception occurred.</param>
        /// <param name="errorCode">The error code associated with the exception.</param>
        /// <param name="consoleOnly">Whether to log only to the console.</param>
#if NET6_0_OR_GREATER
        public void LogException(Exception ex, string? context = null, string? errorCode = null, bool consoleOnly = false)
#else
        public void LogException(Exception ex, string context = "", string errorCode = "", bool consoleOnly = false)
#endif
        {
            if (ex == null)
                return;

            var data = new Dictionary<string, object>();

            // Add error code if provided
            if (!string.IsNullOrEmpty(errorCode))
            {
                data["ErrorCode"] = errorCode;
            }

            // Add exception type
            data["ExceptionType"] = ex.GetType().Name;

            // Add stack trace
            if (ex.StackTrace != null)
            {
                data["StackTrace"] = ex.StackTrace;
            }

            // Add inner exception if present
            if (ex.InnerException != null)
            {
                data["InnerException"] = ex.InnerException.Message;
            }

            // Format the message with context if provided
            string message = !string.IsNullOrEmpty(context)
                ? $"{context}: {ex.Message}"
                : ex.Message;

            // Log the error
            LogError(message, consoleOnly, errorCode, data);

            // Log stack trace as a separate message for better readability
            if (ex.StackTrace != null)
            {
                LogMessage($"Stack trace: {ex.StackTrace}", isError: true, consoleOnly: consoleOnly);
            }
        }

        /// <summary>
        /// Gets a user-friendly error message for the specified error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <returns>A user-friendly error message.</returns>
        public string GetErrorMessage(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode))
                return "Unknown error";

            if (ErrorCodes.TryGetValue(errorCode, out string message))
            {
                return message;
            }
            return $"Unknown error code: {errorCode}";
        }

        /// <summary>
        /// Gets a user-friendly error message with suggestions for the specified error code.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="additionalContext">Additional context to include in the message.</param>
        /// <returns>A user-friendly error message with suggestions.</returns>
#if NET6_0_OR_GREATER
        public string GetUserFriendlyErrorMessage(string errorCode, string? additionalContext = null)
#else
        public string GetUserFriendlyErrorMessage(string errorCode, string additionalContext = "")
#endif
        {
            if (string.IsNullOrEmpty(errorCode))
                return "An unknown error occurred. Please try again or contact support.";

            if (!ErrorCodes.TryGetValue(errorCode, out string message))
            {
                return $"An error occurred (Code: {errorCode}). Please try again or contact support.";
            }

            // Add suggestions based on error code prefix
            string suggestion = string.Empty;

            if (errorCode.StartsWith("CFG-"))
            {
                suggestion = "Try checking your configuration settings or reinstalling the application.";
            }
            else if (errorCode.StartsWith("CSV-"))
            {
                suggestion = "Try checking the CSV file format or recreating the file.";
            }
            else if (errorCode.StartsWith("FS-"))
            {
                suggestion = "Try checking file permissions or disk space.";
            }
            else if (errorCode.StartsWith("NET-"))
            {
                suggestion = "Try checking your network connection or firewall settings.";
            }
            else if (errorCode.StartsWith("APP-"))
            {
                suggestion = "Try restarting the application.";
            }

            // Combine message, context, and suggestion
            string result = message;

            if (!string.IsNullOrEmpty(additionalContext))
            {
                result += $" Context: {additionalContext}.";
            }

            if (!string.IsNullOrEmpty(suggestion))
            {
                result += $" {suggestion}";
            }

            return result;
        }
    }
}
