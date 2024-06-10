using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    public class SetCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string option;
        private readonly string value;

        public string CommandName => "SetOption";

        public string CommandArgs => "option value";

        public string[] CommandAlternates => [];

        public string Description => "Sets a system option to the specified value";

        public string ExtendedDescription => "EG: setoption loglevel debug, setoption progress on";

        public SetCommand()
            : base(Globals.Logger)
        {
            this.option = "";
            this.value = "";
        }
        public SetCommand(string option, string value)
            : base(Globals.Logger)
        {
            this.option = option;
            this.value = value;
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new SetCommand();
            else if (args.Length == 3)
                return new SetCommand(args[1], args[2]);
            else
                return new BadCommand($"Invalid set option/value specified: {string.Join(' ', args)}");
        }

        public void Run()
        {
            switch (option.Trim().ToLower())
            {
                case "loglevel":
                    switch (value.Trim().ToLower())
                    {
                        case "":
                            Log.Info("Sets the logging level. EG: set loglevel debug");
                            break;
                        case "debug": Globals.Logger.SetLogLevel(ICommandLogger.LogLevelType.Debug); break;
                        case "info": Globals.Logger.SetLogLevel(ICommandLogger.LogLevelType.Info); break;
                        case "warning": Globals.Logger.SetLogLevel(ICommandLogger.LogLevelType.Warning); break;
                        case "error": Globals.Logger.SetLogLevel(ICommandLogger.LogLevelType.Error); break;
                        default:
                            Log.Error($"Invalid loglevel: must be one of [ debug, info, warning, error ]");
                            break;
                    }
                    break;
                case "progress":
                    switch (value.Trim().ToLower())
                    {
                        case "":
                            Log.Info("Sets script progress indicators to on or off EG: set progress on");
                            break;
                        case "on":
                            Globals.ShowScriptProgress = true;
                            break;
                        case "off":
                            Globals.ShowScriptProgress = false;
                            break;
                        default:
                            Log.Error("Invalid progress value: must be either on or off");
                            break;
                    }
                    break;
                default:
                    Log.Error($"Unkown set option: {option}");
                    break;
            }
        }
    }
}
