using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    public class PrintCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string message;

        public string CommandName => "Print";

        public string CommandArgs => "message";

        public string[] CommandAlternates => [];

        public string Description => "Prints a message to the output";

        public string ExtendedDescription => "";
        public PrintCommand()
            : base(Globals.Logger)
        {
            message = "";
        }
        public PrintCommand(string message)
            : base(Globals.Logger)
        {
            this.message = message;
        }
        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new PrintCommand();
            else
                return new PrintCommand(string.Join(" ", args.Skip(1)));
        }

        public void Run()
        {
            Log.Prompt($"\n{message}");
        }
    }
}
