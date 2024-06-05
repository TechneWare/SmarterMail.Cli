﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// The default logger for commands
    /// </summary>
    public class CommandLogger : ICommandLogger
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
            this._loglevel = level;
            Debug($"LogLevel changed to: {level}");
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
                Console.WriteLine($"{tStamp}{lvl}: {message}");
            }
        }

        /// <summary>
        /// Writes a message to the output reguardless of log level
        /// Omits timestamps, log levels and new lines
        /// </summary>
        /// <param name="message">The message to display as a prompt</param>
        public void Prompt(string message)
        {
            Console.Write(message);
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
    }
}