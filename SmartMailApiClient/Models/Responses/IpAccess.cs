using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Responses
{
    public class IpAccess
    {
        public string ip { get; set; } = "";
        public int protocolMask { get; set; }
        public string description { get; set; } = "";
        public bool smtp {  get; set; }
        public bool pop {  get; set; }
        public bool imap { get; set; }
        public bool xmpp { get; set; }
        public bool disabledGreylisting { get; set; }
        public bool? bypass { get; set; }
        public bool spam_bypass { get; set; }

        public bool IsSubnet => ip.Contains('/');
    }
}
