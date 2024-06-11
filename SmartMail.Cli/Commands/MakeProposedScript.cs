using NetTools;
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
            if (!Cache.IsLoaded)
            {
                // Reset the cache
                var curLogLevel = Log.LogLevel;
                Log.SetLogLevel(ICommandLogger.LogLevelType.Warning);
                var loadData = new LoadBlockedIpDataCommand().MakeCommand([]);
                loadData.Run();
                Log.SetLogLevel(curLogLevel);
            }

            Log.Debug("---- Building Propossed IP block Script ----");
            var script = new Script(Log, "CommitProposedBans");

            try
            {
                var (newCidrs, removedPermaBans) = BuildCIDRs(script);
                var moveIdsCount = MoveIdsBlocks(script);
                var (existingIgnoredCidrCount, existingIgnoredIpsCount) = DropIgnored(script);
                var objectChanges = newCidrs + removedPermaBans + moveIdsCount + existingIgnoredCidrCount + existingIgnoredIpsCount;

                string[] scriptHead = [
                        $"# AUTO GENERATED SCRIPT TO COMMIT PROPOSED IP BANS - [{DateTime.UtcNow} UTC]",
                        $"# Will Create {newCidrs} CIDR groups and remove {removedPermaBans} perma bans",
                        $"# Will Move {moveIdsCount} Temporary IDS blocks to the blacklist",
                        $"# Will Restore {existingIgnoredCidrCount} Ignored CIDR groups",
                        $"# Will Remove {existingIgnoredIpsCount} Ignored IPs",
                        $"# =============================================================================",
                        $"setoption loglevel warning",
                        $"setoption progress on"
                ];

                string[] scriptFoot = [
                        $"setoption loglevel {Log.LogLevel}",
                        $"setoption progress off"
                    ];

                if (script.ScriptLines.Length != 0 && objectChanges > 0)
                {
                    //Since data will have changed, invalidate the cache
                    script.Add("InvalidateCache"); 

                    Log.Info($"New Blocking Script Created");
                    script.List();
                }
                else
                    script.Add("Print No new IPs to analyze at this time");

                script.Save("commit_bans.txt", scriptHead, scriptFoot);

                Log.Debug("---- Done Building Propossed IP block Script ----");
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to build script: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private (int existingIgnoredCidrCount, int existingIgnoredIpsCount) DropIgnored(Script script)
        {
            Log.Debug("Dropping Ignored");

            var ignoredCIDRs = Cache.IgnoredIps
                    .Where(i => i.IsSubnet)
                    .Select(i => IPAddressRange.Parse(i.Ip))
                    .ToList();

            List<BlockedIpGroup> existingIgnoredCIDRs = Cache.BlockedIpGroups
                                                        .Where(g => ignoredCIDRs.Contains(g.IpRange))
                                                        .ToList();

            if (existingIgnoredCIDRs.Count != 0)
                script.Add($"print === Restoring {existingIgnoredCIDRs.Count} Ignored CIDRs");

            foreach (var g in existingIgnoredCIDRs)
                script.Add($"restore {g.Subnet} force");

            List<BlockedIp> ignoredIPs = Cache.AllBlockedIps
                .Where(b => Cache.IgnoredIps.Any(i => i.Ip == b.Ip && !i.IsSubnet))
                .ToList();

            var ignoredViaCIDR = Cache.AllBlockedIps
                .Where(i => ignoredCIDRs.Any(c => c.Contains(System.Net.IPAddress.Parse(i.Ip))))
                .ToList();

            ignoredIPs.AddRange(ignoredViaCIDR);

            if (ignoredIPs.Count != 0)
                script.Add($"print === Removing {ignoredIPs.Count} Ignored IPs");

            foreach (var i in ignoredIPs)
            {
                script.Add($"Print Removing Ignored IP: {i.Ip}");
                if (i.IsTemporary)
                    script.Add($"dt {i.Ip}");
                else
                    script.Add($"DeleteBan {i.Ip}");
            }

            (int existingIgnoredCidrCount, int existingIgnoredIpsCount) result;
            result.existingIgnoredCidrCount = existingIgnoredCIDRs.Count;
            result.existingIgnoredIpsCount = ignoredIPs.Count;

            return result;
        }

        private int MoveIdsBlocks(Script script)
        {
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

            if (newIpBlocks.Count != 0)
                script.Add($"print === Moving {newIpBlocks.Count} temporary IDS block(s) to the blacklist");

            foreach (var tb in newIpBlocks)
            {
                var existingTempBlock = Cache.TempIpBlocks.Where(b => b.ip == tb.Ip).FirstOrDefault();
                if (existingTempBlock != null)
                {
                    if (script.Add($"dt {existingTempBlock.ip}"))
                    {
                        Log.Debug($"Added DeleteTemp on: {tb.Ip}");

                        if (script.Add($"pb {tb.Ip} {DateTime.UtcNow} {tb.Description}"))
                            Log.Debug($"Added PermaBan on: {tb.Ip}");
                    }
                }
                else
                    Log.Warning($"Unable to Locate existing temp block on {tb.Ip}, so skipped it");
            }

            return newIpBlocks.Count;
        }

        private (int newCidrs, int removedPermaBans) BuildCIDRs(Script script)
        {
            Log.Debug("Setting up CIDR groups");
            var ignoredCIDRs = Cache.IgnoredIps
                    .Where(i => i.IsSubnet)
                    .Select(i => IPAddressRange.Parse(i.Ip))
                    .ToList();

            var permaBansToRemove = Cache.ProposedIpGroups
                                .Where(g => !ignoredCIDRs.Contains(g.IpRange))
                                .SelectMany(g => g.BlockedIps)
                                .Where(i => !i.IsTemporary)
                                .Count();

            var newCidrCount = Cache.ProposedIpGroups.Where(g => !ignoredCIDRs.Contains(g.IpRange)).Count();

            if (newCidrCount > 0)
                script.Add($"print === Making {newCidrCount} CIDR Groups and removing {permaBansToRemove} perma bans");

            foreach (var grp in Cache.ProposedIpGroups.Where(g => !ignoredCIDRs.Contains(g.IpRange)))
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
                            var existingTempBlock = Cache.TempIpBlocks.Where(b => b.ip == blk.Ip).FirstOrDefault();
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

            (int newCidrs, int removedPermaBans) result;
            result.newCidrs = newCidrCount;
            result.removedPermaBans = permaBansToRemove;

            return result;
        }
    }
}
