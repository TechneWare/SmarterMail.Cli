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

                var checkScript = new Script(Log, "CheckForChanges");
                checkScript.Add("GetTempIpBlockCounts");   //If any new IDS blocks, cache will be invalid
                checkScript.Add("make");                   //If cache is invalid, data will reload before making the script
                checkScript.Run();

                //If make qued any changes, run the commit script
                if (Cache.QuedChanges > 0)
                {
                    var runScript = new Script(Log, "CommitChanges");
                    runScript.Add("run commit_bans.txt");    //Run the script that was created by 'make'
                    runScript.Run();
                }
            }

            Log.Debug("---- UpdateBlacklist Done ----");
        }
    }
}
