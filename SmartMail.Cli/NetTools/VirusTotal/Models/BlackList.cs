using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.NetTools.VirusTotal.Models
{
    public class BlackList
    {
        public string method { get; set; } = "";
        public string engine_name { get; set; } = "";
        public string category { get; set; } = "";
        public string result { get; set; } = "";
    }
}
