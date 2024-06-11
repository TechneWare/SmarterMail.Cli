using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.NetTools.VirusTotal.Models
{
    public class Attributes
    {
        public long whois_date {  get; set; }
        public string? continent { get; set; }
        public string? as_owner { get; set; }
        public AnalysisStats? last_analysis_stats { get; set; }
        public int reputation {  get; set; }
        public Votes? total_votes { get; set; }
        public string? network { get; set; }
        public string? regional_internet_registry { get; set; }
        //public Dictionary<string, BlackList> last_analysis_results { get; set; }  //Removed due to the space it takes to store
        public string? whois { get; set; }
        public int? asn { get; set; }
        public string? country { get; set; }
        public long? last_analysis_date { get; set; }
        public long? last_modification_date { get; set; }
        public string[]? tags { get; set; }
    }
}
