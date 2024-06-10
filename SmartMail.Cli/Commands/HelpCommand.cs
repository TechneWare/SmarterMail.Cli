using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Displays usage of a specific command
    /// </summary>
    public class HelpCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string[] args;

        public string CommandName => "Help";

        public string CommandArgs => "commandName";

        public string[] CommandAlternates => ["h"];

        public string Description => "Displays help";

        public string ExtendedDescription => "Specify a command name to get help on that command: EG: Help [CommandName]";

        public HelpCommand()
            :base(Globals.Logger)
        {
            args = [];
        }

        public HelpCommand(string[] args)
            : base(Globals.Logger)
        {
            //Save all args that do not point at this command
            this.args = args.Skip(1).ToArray();
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length == 0)
                return new HelpCommand();
            else
                return new HelpCommand(args);
        }

        public void Run()
        {
            if (this.args.Length == 0)
                Utils.PrintUsage(Utils.GetAvailableCommands());
            else
            {
                var parser = new CommandParser(Utils.GetAvailableCommands());
                var cmd = parser.ParseCommand(this.args);
                if (cmd != null)
                {
                    Utils.PrintCommandUsage((ICommandFactory)cmd, showDetails: true);
                }
            }
        }
    }
}
