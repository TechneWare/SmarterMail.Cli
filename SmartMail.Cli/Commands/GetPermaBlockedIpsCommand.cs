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
        private bool showOutput { get; set; } = false;
        public string CommandName => "GetPermaBlockIps";

        public string CommandArgs => "show";

        public string[] CommandAlternates => ["gpbip", "gpb"];

        public string Description => "loads permanently blocked IP addresses to memory";

        public string ExtendedDescription => "Contains blocked protocol flags";

        public GetPermaBlockedIpsCommand()
            : base(Globals.Logger) { }

        public GetPermaBlockedIpsCommand(string[] args)
            : base(Globals.Logger)
        {
            showOutput = false;
            if (args.Length >= 2 && args[1].ToLower() == "show")
                showOutput = true;
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
                Log.Info("Loading Perma Blocked IPs");
                var r = Globals.ApiClient?.GetPermaBlockedIPs().ConfigureAwait(false).GetAwaiter().GetResult();

                if (IsResponseOk(r))
                {
                    Cache.PermaIpBlocks = [.. r!.ipAccessList];
                    Log.Info($"Total Perma Blocks: {Cache.PermaIpBlocks.Count}");
                    if (showOutput)
                    {
                        foreach (var ip in Cache.PermaIpBlocks)
                        {
                            if (Log.LogLevel == ICommandLogger.LogLevelType.Debug)
                                Log.Info($"{ip.ip.PadRight(16)} smtp:{ip.smtp.ToString().PadRight(6)} pop:{ip.pop.ToString().PadRight(6)} imap:{ip.imap.ToString().PadRight(6)} xmpp:{ip.xmpp.ToString().PadRight(6)} => {ip.description}");
                            else
                                Log.Info($"{ip.ip.PadRight(16)} => {ip.description}");
                        }

                        Log.Info("");
                    }
                }
            }
        }
    }
}
