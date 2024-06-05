using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Clears the screen
    /// </summary>
    public class ClearCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "Clear";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["cls"];

        public string Description => "Clears the screen";

        public string ExtendedDescription => "";

        public ClearCommand() 
            : base(Globals.Logger) { }            

        public ICommand MakeCommand(string[] args)
        {
            return new ClearCommand();
        }

        public void Run()
        {
            Console.Clear();
        }
    }
}
