using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Models
{
    /// <summary>
    /// Represents all known data about a blocked IP address as stored in the SmarterMail server
    /// </summary>
    public class BlockedIp
    {
        //The IP Address
        public string Ip { get; set; } = "";
        
        //Identifies this IP as having come from the temporary(IDS) block list
        public bool IsTemporary { get; set; }

        //If this is a temporary block, then it will have some amount of time to live
        public TimeSpan? BlockTimeRemaining { get; set; }

        //Identifies this entry as a subnet if a / appears in its IP address
        //When CIDR entries are in the black list, they return as an IP
        public bool IsSubnet => Ip.Contains('/');

        //If the subnet is in the description, then we can consider it documented
        public bool IsDocumented => !string.IsNullOrEmpty(Subnet.Trim());
        public bool HasDescription => !string.IsNullOrEmpty(Description.Trim());

        public string Description { get; set; } = "";

        //Get these properties by parsing the description
        public string Subnet => GetValue("SN");
        public long ASN => long.TryParse(GetValue("SN"), out long value) ? value : -1;
        public DateTime TimeStamp => DateTime.TryParse(GetValue("TS"), out DateTime result) ? result : DateTime.MinValue;
        public string Protocol => GetValue("P", "UNKNOWN");
        public string Country => GetValue("CTY");
        public double Score => double.TryParse(GetValue("Score"), out double result) ? result : -1;

        /// <summary>
        /// Parses the description for the specified key and returns its value or the default value
        /// </summary>
        /// <param name="key">The key to search for</param>
        /// <param name="defaultValue">The value to use if the key is not found</param>
        /// <returns></returns>
        private string GetValue(string key, string defaultValue = "")
        {
            var result = "";

            if (HasDescription)
            {
                var items = Description.Split(' ');
                foreach (var item in items)
                {
                    var parts = item.Split(':');
                    if (parts[0].ToLower() == key.ToLower())
                    {
                        if (parts.Length == 2)
                        {
                            result = parts[1];
                            break;
                        }
                    }
                }
            }

            return string.IsNullOrEmpty(result) ? defaultValue : result;
        }
    }
}
