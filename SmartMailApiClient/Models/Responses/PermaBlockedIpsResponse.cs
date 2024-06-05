using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Responses
{
    public class PermaBlockedIpsResponse : IResponse
    {
        public int totalCount { get; set; }
        public IpAccess[]? ipAccessList { get; set; }
        public bool success {  get; set; }
        public string? message { get; set; }
        public Error? ResponseError {  get; set; }
    }
}
