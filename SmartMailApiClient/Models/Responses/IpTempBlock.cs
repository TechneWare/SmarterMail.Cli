using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Responses
{
    public class IpTempBlock
    {
        public int count {  get; set; } 
        public string? ip { get; set; }
        public string? ipLocation { get; set; }
        public ServiceType protocol { get; set; }
        public BlockType blockType { get; set; }
        public string? ruleDescription { get; set; }
        public ServiceType ServiceType { get; set; }
        public double secondsLeftOnBlock { get; set; }
    }
}
