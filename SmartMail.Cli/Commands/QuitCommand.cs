using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Quits the application if running in interactive mode
    /// </summary>
    public class QuitCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "Quit";
        public string CommandArgs => "";
        public string[] CommandAlternates => new string[] { "exit" };
        public string Description => "Ends the program";
        public bool WithLogging { get; set; } = false;

        public string ExtendedDescription => "";

        public QuitCommand()
            : base(Globals.Logger) { }
        public ICommand MakeCommand(string[] args)
        {
            return new QuitCommand();
        }

        public void Run()
        {
            Globals.RefreshTimer?.Dispose();
            Globals.ApiClient?.Dispose();
            Globals.IsQuitSignaled = true;

            Log.Info("--Laterz");
        }
    }
}
