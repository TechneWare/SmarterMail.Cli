using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Requests
{
    public class GetTempBlockedIPs
    {
        public ServiceType[] serviceTypes { get; set; }
        public string search { get; set; }
        public IpBlockInfoSortType sortType { get; set; }
        public bool ascending { get; set; }
        public int startindex { get; set; }
        public int count { get; set; }
    }
}
