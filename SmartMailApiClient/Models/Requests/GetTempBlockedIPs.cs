using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Requests
{
    public class GetTempBlockedIPs
    {
        public ServiceType[] serviceTypes { get; set; } = [ServiceType.Smtp];
        public string search { get; set; } = "";
        public IpBlockInfoSortType sortType { get; set; } = IpBlockInfoSortType.ip;
        public bool ascending { get; set; } = false;
        public int startindex { get; set; } = 0;
        public int count { get; set; } = 100;
    }
}
