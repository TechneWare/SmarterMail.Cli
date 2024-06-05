﻿using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Loads all temporary blocks into the cache
    /// </summary>
    public class GetTempBlockedIpsCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "GetTempBlockIps";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["gtbip", "tb"];

        public string Description => "returns temporarily blocked IP addresses";

        public string ExtendedDescription => "Contains location and protocol data";

        public GetTempBlockedIpsCommand()
            : base(Globals.Logger) { }

        public ICommand MakeCommand(string[] args)
        {
            return new GetTempBlockedIpsCommand();
        }

        public void Run()
        {
            if (IsConnectionOk(Globals.ApiClient))
            {
                Log.Info("---- Temp Blocked IPs ----");
                var r = Globals.ApiClient?.GetCurrentlyBlockedIPs().ConfigureAwait(false).GetAwaiter().GetResult();

                if (IsResponseOk(r))
                {
                    Cache.TempIpBlocks = [.. r!.ipBlocks];
                    Log.Info($"Total Temp Blocks: {Cache.TempIpBlocks.Count}");
                    foreach (var ip in Cache.TempIpBlocks)
                    {
                        var timeRemaining = $"{TimeSpan.FromSeconds(ip.secondsLeftOnBlock):d\\:h\\:mm\\:ss}".PadLeft(16);
                        Log.Info($"{timeRemaining} {ip.ip.PadLeft(16)}{ip.ipLocation.PadLeft(20)}{ip.protocol.ToString().PadLeft(5)} {ip.ruleDescription}");
                    }
                }
            }
        }
    }
}