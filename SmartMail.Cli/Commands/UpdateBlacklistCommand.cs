using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    public class UpdateBlacklistCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "UpdateBlacklist";

        public string CommandArgs => "";

        public string[] CommandAlternates => [];

        public string Description => "Builds a blocking script and executes it in one step";

        public string ExtendedDescription => "This is equivelent to running: 'Make' followed by 'run commit_bans.txt'";

        public UpdateBlacklistCommand()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
        }

        public ICommand MakeCommand(string[] args)
        {
            return new UpdateBlacklistCommand();
        }

        public void Run()
        {
            Log.Debug("---- UpdateBlacklist ----");
            if (IsConnectionOk(Globals.ApiClient))
            {
                var updateScript = new Script(Log, "UpdateBlacklist");
                updateScript.Add("make");
                updateScript.Add("run commit_bans.txt");
                updateScript.Run();
            }
            Log.Debug("---- UpdateBlacklist Done ----");
        }
    }
}
