using SmartMail.Cli.Models;
using SmartMailApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Loads the count summary for all temporary blocks (IDS)
    /// </summary>
    public class GetTempBlockedIpCountsCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "GetTempIpBlockCounts";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["gtbc", "tbc"];

        public string Description => "returns the count of temporary IP blocks by service type";

        public string ExtendedDescription => "";

        public GetTempBlockedIpCountsCommand()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
        }

        public ICommand MakeCommand(string[] args)
        {
            return new GetTempBlockedIpCountsCommand();
        }

        public void Run()
        {
            Log.Debug("---- Blocked IP Counts ----");
            if (IsConnectionOk(Globals.ApiClient))
            {
                var r = Globals.ApiClient?.GetCurrentlyBlockedIPsCount().ConfigureAwait(false).GetAwaiter().GetResult();

                if (IsResponseOk(r))
                {
                    Cache.TempBlockCounts = r!.counts ?? [];

                    foreach (var c in Cache.TempBlockCounts.Where(c => c.Value > 0))
                        Log.Info($"{c.Key,20}:{c.Value,5}");

                    Log.Info($"Total IDS(Temporary) blocks:".PadLeft(21) + $"{Cache.TotalTempBlocks}".PadLeft(5) + " Found");

                    //If we have any new IDS blocks, invalidate the cache
                    if (Cache.TotalTempBlocks > 0)
                        Cache.IsValid = false;
                }
            }
            Log.Debug("---- Blocked IP Counts Done ----");
        }
    }
}
