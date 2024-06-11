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

        public string ExtendedDescription => "This is equivalent to running: 'Make' followed by 'run commit_bans.txt'";

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
                updateScript.Run("GetTempIpBlockCounts");   //If any new IDS blocks, cache will be invalid
                updateScript.Add("make");                   //If cache is invalid, data will reload before making the script
                updateScript.Add("run commit_bans.txt");    //Run the script that was created by 'make'
                updateScript.Run();
            }
            Log.Debug("---- UpdateBlacklist Done ----");
        }
    }
}
