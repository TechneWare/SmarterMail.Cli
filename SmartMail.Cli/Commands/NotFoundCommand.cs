using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Returned by the parser when a command cannot be found from the supplied args
    /// </summary>
    internal class NotFoundCommand : CommandBase, ICommand, ICommandFactory
    {

        public string CommandName => "NotFound";

        public string CommandArgs => "";

        public string[] CommandAlternates => [];

        public string Description { get; set; }

        public string ExtendedDescription => "";

        public NotFoundCommand()
            : base(Globals.Logger)
        {
            Description = "Command Not Found: No Input Provided";
        }

        public NotFoundCommand(string commandName)
            :base(Globals.Logger) 
        {
            this.Description = $"Command Not Found: {commandName}";
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length == 0)
                return new NotFoundCommand();
            else
                return new NotFoundCommand(commandName: args[0]);
        }

        public void Run()
        {
            Log.Warning(Description);
        }
    }
}
