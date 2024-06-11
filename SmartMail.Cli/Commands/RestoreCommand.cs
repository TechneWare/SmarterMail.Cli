using SmartMail.Cli.Models;
using SmartMailApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Restores IPs that start with an IP fragment
    /// </summary>
    public class RestoreCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string ipStartsWithString;
        private readonly bool force;

        public string CommandName => "Restore";

        public string CommandArgs => "IpFragment | * force";

        public string[] CommandAlternates => [];

        public string Description => "Restores the black list from previously stored data.";

        public string ExtendedDescription => "Searches for IPs that start with IpFragment or all IPs if * is used.";

        public RestoreCommand()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            ipStartsWithString = string.Empty;
        }

        public RestoreCommand(string ipStartsWithString, bool force)
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            this.ipStartsWithString = ipStartsWithString;
            this.force = force;
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new RestoreCommand();
            else if (args.Length == 2)
                return new RestoreCommand(args[1], false);
            else if (args.Length == 3 && args[2].Equals("force", StringComparison.CurrentCultureIgnoreCase))
                return new RestoreCommand(args[1], true);
            else
                return new BadCommand($"Unable to understand: {string.Join(" ", args)}");
        }

        public void Run()
        {
            if (!IsConnectionOk(Globals.ApiClient))
                return;

            if (!Cache.IsValid)
            {
                var script = new Script(Log, ["load"], "load data");
                script.Parse();
                script.Run();
            }

            var isValidStartsWithString = (!string.IsNullOrEmpty(ipStartsWithString) &&
                                          (ipStartsWithString.Where(c => c == '.').Count() >= 2) ||
                                           ipStartsWithString == "*"
                                          );

            if (!isValidStartsWithString)
            {
                Log.Error($"[{ipStartsWithString}] is not a valid search string, must be '*' or contain at least 2 '.'");
                return;
            }

            var isSelectAll = ipStartsWithString == "*";
            var ipInfoes = Cache.IPAddressInfos;
            var ipGroups = Cache.BlockedIpGroups;

            if (!isSelectAll)
                ipInfoes = Cache.IPAddressInfos
                    .Where(i => !string.IsNullOrEmpty(i.id) && i.id.StartsWith(ipStartsWithString))
                    .ToList();

            var existingIps = Cache.PermaIpBlocks
                .Where(i => !i.IsSubnet)
                .Select(i => i.ip)
                .ToList();

            //Remove entries for the CIDR groups that match
            if (!isSelectAll)
                ipGroups = Cache.BlockedIpGroups
                    .Where(g => g.Subnet.StartsWith(ipStartsWithString))
                    .ToList();

            //find all Virus Total data where the IP is not already in the block list
            var cidrIps = Cache.IPAddressInfos
                .Where(i => ipGroups.Any(g => i.id?.StartsWith(g.SubnetBase) ?? false))
                .ToList();

            var ipsToRestore = ipInfoes
                .Where(i => !string.IsNullOrEmpty(i.id) && !existingIps.Contains(i.id))
                .ToList();
            ipsToRestore.AddRange(cidrIps);

            var restoreScript = new Script(Log, "RestoreIpInfoes");

            //Create an entry for each IP to restore
            foreach (var ip in ipsToRestore)
            {
                var description = ip.GetDescription(ResolveProtocol(ip!.id!));
                if (restoreScript.Add($"pb {ip.id} {description}"))
                    Log.Info($"Restoring: {ip.id}");
                else
                    Log.Warning($"Unable to restore: {ip.id}");
            }

            foreach (var grp in ipGroups)
            {
                Log.Info($"Removing CIDR: {grp.Subnet}");
                restoreScript.Add($"db {grp.Subnet}");
            }

            var okToRun = !Globals.IsInteractiveMode || force;
            if (!okToRun)
            {
                if (restoreScript.ScriptLines.Length != 0)
                {
                    string userResponse = Utils.InputPrompt("Y", "Proceed with Restore? Y/N:").Trim();

                    okToRun = !string.IsNullOrEmpty(userResponse)
                              && userResponse.Length > 0
                              && userResponse[..1].Equals("y", StringComparison.CurrentCultureIgnoreCase);
                }
                else
                    Log.Info($"Everything matching '{ipStartsWithString}' is already restored");
            }

            if (ipsToRestore.Count != 0)
                restoreScript.Add("InvalidateCache");

            if (okToRun)
            {
                //Execute the script
                var curLogLevel = Log.LogLevel;
                Log.SetLogLevel(ICommandLogger.LogLevelType.Warning);
                Log.Prompt("Running");
                restoreScript.Run(".");
                Log.Prompt("Done\n");
                Log.SetLogLevel(curLogLevel);

            }
        }

        private string ResolveProtocol(string ipAddress)
        {
            //Determine what protocol to include in the the new description
            var protocol = "Unknown";

            var ipCached = Cache.PermaIpBlocks.Where(i => i.ip == ipAddress).FirstOrDefault();
            if (ipCached == null)
                Log.Debug($"{ipAddress} does not exist in the servers Blacklist");

            if (ipCached != null)
            {
                protocol = ipCached.description.Contains("smtp", StringComparison.OrdinalIgnoreCase) || ipCached.description.Contains("harvest", StringComparison.OrdinalIgnoreCase) ? "SMTP" : protocol;
                protocol = ipCached.description.Contains("pop", StringComparison.OrdinalIgnoreCase) ? "POP" : protocol;
                protocol = ipCached.description.Contains("imap", StringComparison.OrdinalIgnoreCase) ? "IMAP" : protocol;
                protocol = ipCached.description.Contains("xmpp", StringComparison.OrdinalIgnoreCase) ? "XMPP" : protocol;
            }

            return protocol;
        }
    }
}
