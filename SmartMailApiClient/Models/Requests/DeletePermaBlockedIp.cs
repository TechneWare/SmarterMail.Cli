using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Requests
{
    public class DeletePermaBlockedIp
    {
        public string address { get; set; }
        public int dataType { get; set; } = 1; //Unkown that this is - undocumented but observed in the wild
    }
}
