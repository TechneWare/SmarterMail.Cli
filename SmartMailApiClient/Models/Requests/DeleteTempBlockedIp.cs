using SmartMailApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Requests
{
    public class DeleteTempBlockedIp
    {
        public IpTempBlock[] ipBlocks { get; set; } = [];

    }
}
