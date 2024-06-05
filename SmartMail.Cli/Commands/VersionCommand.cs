using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Displays the current build information
    /// </summary>
    public class VersionCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "Version";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["ver", "v"];

        public string Description => "Display Version Info";

        public bool WithLogging { get; set; }

        public string ExtendedDescription => "";

        public VersionCommand()
            :base(Globals.Logger) { RequiresInteractiveMode = false; }

        public ICommand MakeCommand(string[] args)
        {
            return new VersionCommand();
        }

        public void Run()
        {
            var version = Assembly.GetEntryAssembly()!.GetName().Version;

            Log.Info($"SmarterMail API Client Version: {(version?.ToString() ?? "Unknown")}");
        }
    }
}
