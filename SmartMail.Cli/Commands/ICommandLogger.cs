using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// The logger interface used by commands
    /// </summary>
    public interface ICommandLogger
    {
        [JsonConverter(typeof(StringEnumConverter))]
        enum LogLevelType { Debug, Info, Warning, Error }
        public LogLevelType LogLevel { get; }

        public void SetLogLevel(LogLevelType level);
        public void Log(string message);
        public void Log(string message, LogLevelType level);
        public void Prompt(string message);
    
        public void Debug(string message);
        public void Info(string message);
        public void Warning(string message);
        public void Error(string message);

        public ICommandLogger UseTimestamps();
        public ICommandLogger UseLogLevel();
    }
}
