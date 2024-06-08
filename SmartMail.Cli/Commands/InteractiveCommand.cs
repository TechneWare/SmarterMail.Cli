using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// A command to display an interactive shell to the user and allow commands to be issued
    /// </summary>
    public class InteractiveCommand : CommandBase, ICommand, ICommandFactory
    {
        private string[] args;

        public string CommandName => "Interactive";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["shell"];

        public string Description => "Launches the interactive shell";

        public string ExtendedDescription => "Allows user input";

        public InteractiveCommand()
            : base(Globals.Logger)
        {
            this.args = [];
        }
        public InteractiveCommand(string[] args)
            : base(Globals.Logger)
        {
            this.args = args;
        }

        public ICommand MakeCommand(string[] args)
        {
            return new InteractiveCommand(args);
        }

        public void Run()
        {
            Globals.IsInteractiveMode = true;
            var availableCommands = Utils.GetAvailableCommands();
            var parser = new CommandParser(availableCommands);
            parser.ParseCommand(["version"]).Run();

            if (args.Length > 0)
            {
                //avoid calling yourself
                args = args.Skip(1).ToArray();
                if (args.Length > 0)
                    parser.ParseCommand(args).Run();
            }

            if (!Globals.IsQuitSignaled)
            {
                ICommand? lastCommand = null;
                do
                {
                    args = Utils.GetInput(prompt: Utils.DefaultPrompt).Split(' ');

                    if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
                        Utils.PrintUsage(availableCommands);
                    else
                    {
                        ICommand? command = parser.ParseCommand(args);
                        if (command != null)
                        {
                            lastCommand = command;
                            command.Run();
                        }
                    }
                } while ((lastCommand == null || lastCommand.GetType() != typeof(QuitCommand)) && !Globals.IsQuitSignaled);
            }
        }

        
    }
}
