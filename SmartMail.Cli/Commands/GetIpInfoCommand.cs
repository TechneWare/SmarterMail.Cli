using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Simple command to display Virus Total data about an IP address and save the response
    /// </summary>
    public class GetIpInfoCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string ipAddress;

        public string CommandName => "IpInfo";

        public string CommandArgs => "IpAddress";

        public string[] CommandAlternates => ["ip"];

        public string Description => "Attempts to retrieve IP Info from Virus Total for the specified IP Address";

        public string ExtendedDescription => "";

        public GetIpInfoCommand()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            ipAddress = "";
        }
        public GetIpInfoCommand(string IpAddress)
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            ipAddress = IpAddress;
        }
        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new GetIpInfoCommand();
            else
                return new GetIpInfoCommand(args[1]);
        }

        public void Run()
        {
            try
            {
                var ipInfo = Cache.GetIpAddressInfo(ipAddress, withSave: true);
                if (ipInfo != null)
                {
                    Log.Info($"IP Info For: {ipInfo.id}");
                    Log.Info($"Score:{ipInfo!.attributes!.last_analysis_stats.Score}");
                    Log.Info($"Last ApiQuery:{ipInfo.LastQuery} UTC");
                    Log.Info($"Last Analysis:{Cache.UnixTimeToDateTimeUTC(ipInfo.attributes.last_analysis_date)} UTC");
                    Log.Info($"Last Modified:{Cache.UnixTimeToDateTimeUTC(ipInfo.attributes.last_modification_date)} UTC");
                }
                else
                    Log.Info("No IP Info Returned");
            }
            catch (Exception ex)
            {
                Log.Error($"Error getting IpInfo: {ex.Message}");
                if (ex.Message.Contains("QuotaExceededError"))
                    Globals.ExpireAccessVtApi();
            }

        }
    }
}
