using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.NetTools.VirusTotal.Models
{
    public class IPAddressInfo
    {
        public string id { get; set; }
        public string type { get; set; }
        public Attributes attributes { get; set; }
        public DateTime LastQuery { get; set; }
    }
}
