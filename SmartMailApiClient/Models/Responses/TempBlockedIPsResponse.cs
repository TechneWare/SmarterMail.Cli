using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Responses
{
    public class TempBlockedIPsResponse : IResponse
    {
        public int count { get; set; }
        public IpTempBlock[]? ipBlocks { get; set; }
        public bool success { get; set; }
        public string? message { get; set; }
        public Error? ResponseError { get; set; }
    }
}
