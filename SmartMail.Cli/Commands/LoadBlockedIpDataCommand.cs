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
            : base(Globals.Logger) 
        {
            IsThreadSafe = false;
        }

        public ICommand MakeCommand(string[] args)
        {
            return new LoadBlockedIpDataCommand();
        }

        public void Run()
        {
            if (!IsConnectionOk(Globals.ApiClient))
                return;

            Log.Debug("---- Loading IP block data ----");

            var getDataScript = new Script(Log, "LoadData");
            getDataScript.Load([
                    "GetTempBlockIps",
                    "GetPermaBlockIps"
                ]);

            Cache.IsValid = false;
            getDataScript.Run();

            Log.Info($"Found {Cache.TempIpBlocks.Count} Temp Blocks");
            Log.Info($"Found {Cache.PermaIpBlocks.Count} Perma Blocks");

            try
            {
                Log.Debug("--- Reloading Cache ---");
                Cache.Init(Cache.TempIpBlocks, Cache.PermaIpBlocks);
                Log.Debug("--- Reloading Cache Done ---");

                Log.Info($"Total Known IPs:{Cache.AllBlockedIps.Count}\tExisting Groups:{Cache.BlockedIpGroups.Count}");
                Log.Debug($"IPs added to existing groups:{Cache.BlockedIpGroups.SelectMany(g => g.BlockedIps).Count()}");

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

                Cache.IsValid = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed Loading IP block data: {ex.Message}");
            }
            finally
            {
                Log.Debug("---- Loading IP block data Done ----");
            }
        }
    }
}
