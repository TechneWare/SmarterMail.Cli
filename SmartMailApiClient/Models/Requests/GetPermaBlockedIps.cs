using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Requests
{
    public class GetPermaBlockedIps
    {
        public SearchParams searchParams {  get; set; }
        public bool showHoneypot {  get; set; }
    }
}
