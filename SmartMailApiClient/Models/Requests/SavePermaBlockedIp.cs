using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Requests
{
    public class SavePermaBlockedIp
    {
        public ServiceType[]? serviceList { get; set; }
        public IPAccessDataType dataType { get; set; }
        public string? address { get; set; }
        public string? oldAddress { get; set; }
        public string? description { get; set; }
        public bool spam_bypass { get; set; }
    }
}
