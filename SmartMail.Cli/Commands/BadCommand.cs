using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Command that is returned when bad arguments are used
    /// </summary>
    internal class BadCommand : CommandBase, ICommand, ICommandFactory
    {
        public string Message { get; set; }

        public string CommandName => "BadCommand";

        public string CommandArgs => "";

        public string[] CommandAlternates => new string[] { };

        public string Description => "Command not understood";
        public bool WithLogging { get; set; } = false;

        public string ExtendedDescription => "";

        public BadCommand() 
            : base(Globals.Logger) 
        { Message = ""; }
        public BadCommand(string message)
            : base(Globals.Logger)
        {
            Message = message;
        }

        public void Run()
        {
           Log.Warning($"Bad Command=> {Message}");
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new BadCommand();
            else
                return new BadCommand(args[1]);
        }
    }
}
