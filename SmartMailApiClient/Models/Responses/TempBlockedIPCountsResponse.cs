using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Responses
{
    public class TempBlockedIPCountsResponse: IResponse
    {
        public Dictionary<string, int> counts {  get; set; }
        public string message { get; set; }
        
        public bool success { get; set; }

        public Error? ResponseError { get; set; }
    }
}
