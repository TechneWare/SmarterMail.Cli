using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Builds a script(commit_bans.txt) to execute the currently proposed new blocking strategy
    /// </summary>
    public class MakeProposedScript : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => $"MakeScript";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["make"];

        public string Description => "Makes a commit script out of the proposed bans to be executed later";

        public string ExtendedDescription => "";

        public MakeProposedScript()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
        }

        public ICommand MakeCommand(string[] args)
        {
            return new MakeProposedScript();
        }

        public void Run()
        {
            var curLogLevel = Log.LogLevel;
            Log.SetLogLevel(ICommandLogger.LogLevelType.Warning);
            // Reset the cache and generate a new blocking strategy
            var loadData = new LoadBlockedIpDataCommand().MakeCommand([]);
            loadData.Run();
            Log.SetLogLevel(curLogLevel);

            Log.Debug("---- Building Propossed IP block Script ----");
            var script = new Script(Log, "CommitProposedBans");

            try
            {
                Log.Debug("Setting up CIDR groups");

                var permaBansToRemove = Cache.ProposedIpGroups.SelectMany(g => g.BlockedIps).Where(i => !i.IsTemporary).Count();
                var allTempBlocks = Cache.TempIpBlocks;
               
                if (Cache.ProposedIpGroups.Any())
                    script.Add($"print === Making {Cache.ProposedIpGroups.Count} CIDR Groups and removing {permaBansToRemove} perma bans");
                
                foreach (var grp in Cache.ProposedIpGroups)
                {
                    //Create the new group, assume not using Virus Total api (No Score Avaialable)
                    var description = $"{DateTime.UtcNow}| IPs[{grp.BlockedIps.Count}] %Abuse[{grp.PercentAbuse:P}]";

                    //Using the Virus Total api, so include the score
                    if (!string.IsNullOrEmpty(Globals.Settings.VirusTotalApiKey))
                        description = $"{DateTime.UtcNow}| IPs[{grp.BlockedIps.Count}] AvgScore[{grp.AvgScore:F3}] %Abuse[{grp.PercentAbuse:P}]";

                    if (script.Add($"pb {grp.Subnet} {description}"))  //Perma ban the CIDR with a limited description
                    {
                        Log.Debug($"Added Entry for CIDR:{grp.Subnet} to clean up {grp.BlockedIps.Count} IP blocks");

                        foreach (var blk in grp.BlockedIps)
                        {
                            if (blk.IsTemporary)
                            {
                                //remove temp ban
                                var existingTempBlock = allTempBlocks.Where(b => b.ip == blk.Ip).FirstOrDefault();
                                if (existingTempBlock != null && script.Add($"DeleteTemp {existingTempBlock.ip}"))
                                    Log.Debug($"Added DeleteTemp on {blk.Ip}");
                            }
                            else
                            {
                                //remove perma ban
                                if (script.Add($"DeleteBan {blk.Ip}"))
                                    Log.Debug($"Cleaned up Perma Block on: {blk.Ip}");
                            }
                        }
                    }

                }

                //move remaining temp bans to perma bans
                Log.Debug("Setting up individual bans");
                //Select all temp IPs that are not part of a group
                var newIps = Cache.AllBlockedIps           //De-duping list, if multiple protocols are hit - returns List<string>
                    .Where(b => b.IsTemporary
                             && !Cache.ProposedIpGroups
                                      .SelectMany(g => g.BlockedIps)
                                      .Any(gp => gp.Ip == b.Ip))
                    .Select(b => b.Ip)
                    .Distinct().ToList();

                var newIpBlocks = newIps                //Returns a list of newly BlockedIP objects that are distinct on IP
                    .Select(i => Cache.AllBlockedIps.First(c => c.Ip == i))
                    .ToList();

                if (newIpBlocks.Any())
                    script.Add($"print === Moving {newIpBlocks.Count} temporary IDS block(s) to the blacklist");

                foreach (var tb in newIpBlocks)
                {
                    var existingTempBlock = allTempBlocks.Where(b => b.ip == tb.Ip).FirstOrDefault();
                    if (existingTempBlock != null)
                    {
                        if (script.Add($"dt {existingTempBlock.ip}"))
                        {
                            Log.Debug($"Added DeleteTemp on: {tb.Ip}");

                            ResolveProtocol(tb);

                            if (script.Add($"pb {tb.Ip} {DateTime.UtcNow} {tb.Description}"))
                                Log.Debug($"Added PermaBan on: {tb.Ip}");
                        }
                    }
                    else
                        Log.Warning($"Unable to Locate existing temp block on {tb.Ip}, so skipped it");
                }

                string[] scriptHead = [
                        $"# AUTO GENERATED SCRIPT TO COMMIT PROPOSED IP BANS - [{DateTime.UtcNow} UTC]",
                        $"# Will Create {Cache.ProposedIpGroups.Count} CIDR groups and remove {permaBansToRemove} perma bans",
                        $"# Will Move {newIpBlocks.Count} Temporary IDS blocks to the blacklist",
                         "# ============================================================================="
                ];

                if (script.ScriptLines.Any())
                {
                    script.Add("InvalidateCache"); //Since data may have changed, invalidate the cache
                    Log.Info($"New Blocking Script Created");
                    script.List();
                }
                else
                    script.Add("Print No new IPs to analyze at this time");

                script.Save("commit_bans.txt", scriptHead);

                Log.Debug("---- Done Building Propossed IP block Script ----");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to build script: {ex.Message}");
            }
        }

        /// <summary>
        /// Resolve the best protocol to use in the description
        /// </summary>
        /// <param name="tb">A temporarily blocked IP (IDS)</param>
        private static void ResolveProtocol(BlockedIp? tb)
        {
            var protocol = "UNKNOWN";
            if (tb != null)
            {
                protocol = tb.Description.ToLower().Contains("smtp") || tb.Description.ToLower().Contains("harvest") ? "SMTP" : protocol;
                protocol = tb.Description.ToLower().Contains("pop") ? "POP" : protocol;
                protocol = tb.Description.ToLower().Contains("imap") ? "IMAP" : protocol;
                protocol = tb.Description.ToLower().Contains("xmpp") ? "XMPP" : protocol;
            }
        }
    }
}
