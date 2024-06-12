using SmartMail.Cli.Models;
using SmartMailApiClient.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Loads the cache with all currently black listed IPs
    /// </summary>
    public class GetPermaBlockedIpsCommand : CommandBase, ICommand, ICommandFactory
    {
        private bool ShowOutput { get; set; } = false;
        public string CommandName => "GetPermaBlockIps";

        public string CommandArgs => "show";

        public string[] CommandAlternates => ["gpbip", "gpb"];

        public string Description => "loads permanently blocked IP addresses to memory";

        public string ExtendedDescription => "Contains blocked protocol flags";

        public GetPermaBlockedIpsCommand()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
        }

        public GetPermaBlockedIpsCommand(string[] args)
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            ShowOutput = false;
            if (args.Length >= 2 && args[1].Equals("show", StringComparison.OrdinalIgnoreCase))
                ShowOutput = true;
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new GetPermaBlockedIpsCommand();
            else
                return new GetPermaBlockedIpsCommand(args);
        }

        public void Run()
        {
            if (IsConnectionOk(Globals.ApiClient))
            {
                Log.Debug("--- Get Perma Blocked IPs ---");

                int pageSize = 1000;
                int skip = 0;
                Cache.PermaIpBlocks.Clear();

                var r = Globals.ApiClient?.GetPermaBlockedIPs(pageSize, skip).ConfigureAwait(false).GetAwaiter().GetResult();
                while (IsResponseOk(r) && r!.ipAccessList!.Length == pageSize)
                {
                    Cache.PermaIpBlocks.AddRange(r.ipAccessList);
                    skip += pageSize;
                    r = Globals.ApiClient?.GetPermaBlockedIPs(pageSize, skip).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                if (IsResponseOk(r))
                {
                    Cache.PermaIpBlocks.AddRange(r!.ipAccessList!);
                    Log.Info($"Total Perma Blocks: {Cache.PermaIpBlocks.Count}");
                    if (ShowOutput)
                    {
                        foreach (var ip in Cache.PermaIpBlocks)
                        {
                            if (Log.LogLevel == ICommandLogger.LogLevelType.Debug)
                                Log.Info($"{ip.ip,-16} smtp:{ip.smtp,-6} pop:{ip.pop,-6} imap:{ip.imap,-6} xmpp:{ip.xmpp,-6} => {ip.description}");
                            else
                                Log.Info($"{ip.ip,-16} => {ip.description}");
                        }

                        Log.Info("");
                    }
                }

                Log.Debug("--- Get Perma Blocked IPs Done ---");
            }
        }
    }
}
