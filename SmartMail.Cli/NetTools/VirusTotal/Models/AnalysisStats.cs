using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.NetTools.VirusTotal.Models
{
    public class AnalysisStats
    {
        public int malicious { get; set; }
        public int suspicious { get; set; }
        public int undetected { get; set; }
        public int harmless { get; set; }
        public int timeout { get; set; }

        public double Total => (double)(malicious + suspicious + undetected + harmless + timeout);
        public double PositiveTotal => (double)(undetected + harmless + timeout);
        public double NegativeTotal => (double)(malicious + suspicious);
        public double Score => NegativeTotal / PositiveTotal;
    }
}
