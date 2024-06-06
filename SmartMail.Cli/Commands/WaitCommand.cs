using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Waits the specified number of milliseconds
    /// Useful in scripts
    /// </summary>
    public class WaitCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly int milliseconds;

        public string CommandName => "Wait";

        public string CommandArgs => "milliseconds";

        public string[] CommandAlternates => [];

        public string Description => "Waits the specified number of milliseconds";

        public string ExtendedDescription => "";

        public WaitCommand()
            : base(Globals.Logger)
        {

        }

        public WaitCommand(int milliseconds)
            : base(Globals.Logger)
        {
            this.milliseconds = milliseconds;
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new WaitCommand();
            else if (int.TryParse(args[1], out int waitTime))
                return new WaitCommand(waitTime);
            else
                return new BadCommand("milliseconds must be a number");
        }

        public void Run()
        {
            System.Threading.Thread.Sleep(milliseconds);
        }
    }
}
