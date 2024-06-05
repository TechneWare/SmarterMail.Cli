using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.NetTools.VirusTotal.Models
{
    public class Votes
    {
        public int harmless {  get; set; }
        public int malicious { get; set; }
    }
}
