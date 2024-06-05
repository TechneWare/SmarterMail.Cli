using SmartMail.Cli.Models;
using SmartMailApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Initilizes the cache with all IP block data and displays the proposed new blocking strategy
    /// </summary>
    public class LoadBlockedIpDataCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "LoadBlockedIpData";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["load"];

        public string Description => "Loads all sources of blocked IP data into memory";

        public string ExtendedDescription => "Data is anyalized with find subnets to identify groups of malicious IPs";

        public LoadBlockedIpDataCommand()
            : base(Globals.Logger) { }

        public ICommand MakeCommand(string[] args)
        {
            return new LoadBlockedIpDataCommand();
        }

        public void Run()
        {
            if (!IsConnectionOk(Globals.ApiClient))
                return;

            Log.Info("---- Loading IP block data ----");

            var getDataScript = new Script(Log, "LoadData");
            getDataScript.Load([
                    "GetTempIpBlockCounts",
                    "GetTempBlockIps",
                    "GetPermaBlockIps"
                ]);

            Cache.IsLoaded = false;
            getDataScript.Run();

            Log.Info($"Found {Cache.TempIpBlocks.Count} Temp Blocks");
            Log.Info($"Found {Cache.PermaIpBlocks.Count} Perma Blocks");

            try
            {
                Cache.Init(Cache.TempIpBlocks, Cache.PermaIpBlocks);

                Log.Debug("Cache Loaded");

                Log.Info($"Total IPs:{Cache.AllBlockedIps.Count}\tExisting Groups:{Cache.BlockedIpGroups.Count}");
                Log.Info($"IPs added to existing groups:{Cache.BlockedIpGroups.SelectMany(g => g.BlockedIps).Count()}");
                Log.Info($"Proposed: {Cache.ProposedIpGroups.SelectMany(g => g.BlockedIps).Count()} IPs in {Cache.ProposedIpGroups.Count} New Groups");

                var existingIPblocksToLeave = Cache.AllBlockedIps
                    .Where(b => !b.IsTemporary
                             && !Cache.ProposedIpGroups
                                      .SelectMany(g => g.BlockedIps)
                                      .Any(gp => gp.Ip == b.Ip)).Count();

                var existingIPblocksToRemove = Cache.ProposedIpGroups
                                                    .SelectMany(g => g.BlockedIps)
                                                    .Where(i => !i.IsTemporary).Count();

                var newIpBlocks = Cache.AllBlockedIps
                    .Where(b => b.IsTemporary
                             && !Cache.ProposedIpGroups
                                      .SelectMany(g => g.BlockedIps)
                                      .Any(gp => gp.Ip == b.Ip)).Count();

                Log.Info($"Proposed: Leave {existingIPblocksToLeave} perma blocks and Remove {existingIPblocksToRemove} perma blocks");
                Log.Info($"Proposed: Create {newIpBlocks} new Perma blocks");

                Cache.IsLoaded = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed Loading IP block data: {ex.Message}");
            }
            finally
            {
                Log.Info("-------------------------------");
            }
        }


    }
}
