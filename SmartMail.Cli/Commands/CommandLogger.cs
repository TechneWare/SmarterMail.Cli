using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// The default logger for commands
    /// </summary>
    public partial class CommandLogger : ICommandLogger
    {
        private bool useTimeStamps = false;  //If true, include timestamps in the output
        private bool useLogLevel = false;    //If true, include the logging lvel in the output
        private ICommandLogger.LogLevelType _loglevel;  //the current log level of this logger

        public CommandLogger()
        { }

        //The current log level of this logger
        public ICommandLogger.LogLevelType LogLevel => _loglevel;

        /// <summary>
        /// Sets the current log level for the logger
        /// </summary>
        /// <param name="level">The level to set</param>
        public void SetLogLevel(ICommandLogger.LogLevelType level)
        {
            var oldLevel = this.LogLevel;
            this._loglevel = level;
            Debug($"LogLevel changed to: {level}");

            if (oldLevel == ICommandLogger.LogLevelType.Debug && level != ICommandLogger.LogLevelType.Debug)
                Info($"LogLevel changed to: {level}");
        }

        /// <summary>
        /// Logs a message using the current log level
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Log(string message)
        {
            Log(message, this.LogLevel);
        }

        /// <summary>
        /// Logs a message if the request log level is >= the current log level
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The log level olf the message</param>
        public void Log(string message, ICommandLogger.LogLevelType level)
        {
            if (level >= this.LogLevel)
            {
                var tStamp = useTimeStamps ? $"[{DateTime.UtcNow} UTC]" : "";
                var lvl = useLogLevel ? level.ToString().PadRight(10) : "";
                var output = UniCode202Regex().Replace($"{tStamp}{lvl}: {message}", " ");
                Console.WriteLine(output);
                LogToFile(output);
            }
        }

        /// <summary>
        /// Writes a message to the output reguardless of log level
        /// Omits timestamps, log levels and new lines
        /// </summary>
        /// <param name="message">The message to display as a prompt</param>
        public void Prompt(string message)
        {
            message = UniCode202Regex().Replace(message, " ");
            Console.Write(message);
            LogToFile(message, AsText: true);
        }

        public void LogToFile(string message, bool AsText = false)
        {
            if (Globals.Settings.UseFileLogging)
            {
                try
                {
                    if (File.Exists(Settings.LogFileName))
                    {
                        var fInfo = new FileInfo(Settings.LogFileName);
                        if (fInfo != null && fInfo.Length >= Globals.Settings.MaxLogSizeKB * 1024)
                        {
                            //Splilt the log
                            var splitFileName = Settings.LogFileNameTimeStamped;
                            File.Copy(Settings.LogFileName, splitFileName);
                            File.WriteAllText(Settings.LogFileName, string.Empty);
                            File.AppendAllLines(Settings.LogFileName, [$"Log Split to: {splitFileName}"]);
                        }

                        string[] logFiles = [];
                        do
                        {
                            if (logFiles.Length > 0)
                                File.Delete(logFiles.First());

                            logFiles = [.. Directory.GetFiles(Settings.path, "SmartMail.Cli*.log")
                                            .Where(f => f != "SmartMail.Cli.log")
                                            .OrderBy(f => f)];

                        } while (logFiles.Length > Globals.Settings.MaxLogFiles);

                    }

                    if (AsText)
                        File.AppendAllText(Settings.LogFileName, message);
                    else
                        File.AppendAllLines(Settings.LogFileName, [message]);
                }
                catch (Exception ex)
                {
                    Error($"==> Error Logging to File: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Log the message at the Debug level
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Debug(string message)
        {
            Log(message, ICommandLogger.LogLevelType.Debug);
        }
        /// <summary>
        /// Log the message at the Info level
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Info(string message)
        {
            Log(message, ICommandLogger.LogLevelType.Info);
        }
        /// <summary>
        /// Log the message at the Warning level
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Warning(string message)
        {
            Log(message, ICommandLogger.LogLevelType.Warning);
        }
        /// <summary>
        /// Log the message at the Error level
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Error(string message)
        {
            Log(message, ICommandLogger.LogLevelType.Error);
        }

        /// <summary>
        /// Configures the logger to use timestamps in the output
        /// </summary>
        /// <returns>The current ICommandLogger</returns>
        public ICommandLogger UseTimestamps()
        {
            this.useTimeStamps = true;
            return this;
        }

        /// <summary>
        /// Configures the logger to use log levels in the output
        /// </summary>
        /// <returns>The current ICommandLogger</returns>
        public ICommandLogger UseLogLevel()
        {
            this.useLogLevel = true;
            return this;
        }

        [GeneratedRegex(@"[\u202F]")]
        private static partial Regex UniCode202Regex();
    }
}
