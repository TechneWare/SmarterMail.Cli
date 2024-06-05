using SmartMail.Cli.Models;
using SmartMail.Cli.NetTools.VirusTotal.Models;
using SmartMailApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Documents an IP address by requesting it's data from Virus Total and updating it's description on the server
    /// </summary>
    public class DocumentIPCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string ipAddress;
        private string protocol;
        private readonly bool allowCacheReload;
        private readonly bool allowSaving;

        public string CommandName => "Doc";

        public string CommandArgs => "IpAddress protocol noload nosave";

        public string[] CommandAlternates => [];

        public string Description => "Adds known IP Info to an IPs description in the servers settings>security>black list";

        public string ExtendedDescription => "\n(Uses Virus Total API) EG: Description => Subnet:[127.0.0.0/24] TS:[YYYY/MM/DD HH:MM:SS UTC] P:[Smtp, Pop etc.] CTY:[CN, US, etc.] Score:[.152]" +
                                             "\nnoload prevents cache reloading at completion" +
                                             "\nnosave prevent saving after accessing the VirusTotal api" +
                                             "\nYou will need to call SaveIpInfo manually to persist the VirusTotal response data";

        public DocumentIPCommand()
            : base(Globals.Logger)
        {
            this.ipAddress = "";
            this.protocol = "UNKNOWN";
            this.allowCacheReload = true;
            this.allowSaving = true;
        }
        public DocumentIPCommand(string ipAddress)
            : base(Globals.Logger)
        {
            this.ipAddress = ipAddress;
            this.protocol = "UNKNOWN";
            this.allowCacheReload = true;
            this.allowSaving = true;
        }
        public DocumentIPCommand(string ipAddress, string protocol)
            : base(Globals.Logger)
        {
            this.ipAddress = ipAddress;
            this.protocol = protocol;
            this.allowCacheReload = true;
            this.allowSaving = true;
        }
        public DocumentIPCommand(string ipAddress, string protocol, bool allowCacheReload, bool allowSaving)
            : base(Globals.Logger)
        {
            this.ipAddress = ipAddress;
            this.protocol = protocol;
            this.allowCacheReload = allowCacheReload;
            this.allowSaving = allowSaving;
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new DocumentIPCommand();
            else if (args.Length == 2)
                return new DocumentIPCommand(args[1]);
            else if (args.Length == 3)
                return new DocumentIPCommand(args[1], args[2]);
            else
            {
                var allowReload = true;
                var allowSaving = true;
                if (args.Any(a => a.ToLower() == "noload")) allowReload = false;
                if (args.Any(a => a.ToLower() == "nosave")) allowSaving = false;

                return new DocumentIPCommand(args[1], args[2], allowReload, allowSaving);
            }
        }

        public void Run()
        {
            if (Globals.TryAccessVtApi())
            {
                try
                {
                    //make sure cache is loaded first
                    var loadCacheScript = new Script(Log, ["load"], "LoadCache");
                    if (!Cache.IsLoaded)
                    {
                        loadCacheScript.Parse();
                        loadCacheScript.Run();
                    }

                    var ipCached = Cache.PermaIpBlocks.Where(i => i.ip == ipAddress).FirstOrDefault();
                    if (ipCached == null)
                        Log.Warning($"{ipAddress} does not exist in the servers Blacklist");

                    var ipInfo = Cache.GetIpAddressInfo(ipAddress, withSave: allowSaving);
                    if (ipInfo == null)
                        Log.Warning($"Unable to find any information for {ipAddress}");

                    ResolveProtocol(ipCached);
                    string description = GetIpDescription(ipInfo);

                    //Build the script to run the block and set the description
                    var script = new Script(Log, $"DOC {ipAddress}");
                    if (script.Add($"pb {ipAddress} {description}"))
                    {
                        script.Run();

                        //Reload the cache when done if the caller didn't specify skipping it
                        //If several of these commands are qued up, then you might want to run them without cache reloading until all have executed
                        //Otherwise if its just a single command, then you probably do want to reload the cache when its done.
                        if (allowCacheReload)
                            loadCacheScript.Run();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error getting info for {ipAddress}: {ex.Message}");
                    if (ex.Message.Contains("QuotaExceededError"))
                        Globals.ExpireAccessVtApi();
                }
            }
        }

        /// <summary>
        /// Get the best possible protocol name to describe why this IP is being blocked
        /// </summary>
        /// <param name="ipCached">The IPAccess object that was resolved from cache</param>
        private void ResolveProtocol(IpAccess? ipCached)
        {
            //Determine what protocol to include in the the new description
            //IPs arrive on the black list initially with a limited description from the temporary block
            if ((string.IsNullOrEmpty(protocol) || protocol.ToLower() == "unknown") && ipCached != null)
            {
                protocol = ipCached.description.ToLower().Contains("smtp") || ipCached.description.ToLower().Contains("harvest") ? "SMTP" : protocol;
                protocol = ipCached.description.ToLower().Contains("pop") ? "POP" : protocol;
                protocol = ipCached.description.ToLower().Contains("imap") ? "IMAP" : protocol;
                protocol = ipCached.description.ToLower().Contains("xmpp") ? "XMPP" : protocol;
            }
        }

        /// <summary>
        /// Get's the best description possible for the requested IP
        /// </summary>
        /// <param name="ipInfo">Virus Total data on this IP</param>
        /// <returns>The resolved description for the IP</returns>
        private string GetIpDescription(IPAddressInfo? ipInfo)
        {
            string description = "";
            if (ipInfo != null)
                //Set the description using the Virus Total data - USing key:value format to allow parsing
                description = $"TS:{ipInfo.LastQuery} SN:{ipInfo.attributes.network} ASN:{ipInfo.attributes.asn} P:{protocol} CTY:{ipInfo.attributes.country} Score:{ipInfo.attributes.last_analysis_stats.Score:F3}";
            else
            {
                //Otherwise, (we might have overrun the quota at Virus Total) use a limited description that can be used next time Virus Total is working
                var tempBlock = Cache.TempIpBlocks.Where(i => i.ip == ipAddress).FirstOrDefault();
                if (tempBlock != null)
                    //Use the data from the temporary block if its available
                    description = $"{DateTime.UtcNow} {tempBlock.ipLocation} {tempBlock.ruleDescription}";
                else
                    //Otherwise just use a default description
                    description = $"{DateTime.UtcNow} No Description Available";
            }

            return description;
        }
    }
}
