using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.NetTools.VirusTotal.Models
{
    public class IPAddressInfo
    {
        public string? id { get; set; }
        public string? type { get; set; }
        public Attributes? attributes { get; set; }
        public DateTime LastQuery { get; set; }

        /// <summary>
        /// Get's the best description possible for the requested IP
        /// </summary>
        /// <param name="ipInfo">Virus Total data on this IP</param>
        /// <returns>The resolved description for the IP</returns>
        public string GetDescription(string protocol)
        {
            string description = "";
            var hasData = !string.IsNullOrEmpty(id)
                       && !string.IsNullOrEmpty(type)
                       && attributes != null;


            if (hasData)
                //Set the description using the Virus Total data - USing key:value format to allow parsing
                description = $"TS:{LastQuery} SN:{attributes!.network} ASN:{attributes.asn} P:{protocol} CTY:{attributes.country} Score:{attributes.last_analysis_stats.Score:F3}";
            else
            {
                //Otherwise, (we might have overrun the quota at Virus Total) use a limited description that can be used next time Virus Total is working
                var tempBlock = Cache.TempIpBlocks.Where(i => i.ip == id).FirstOrDefault();
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
