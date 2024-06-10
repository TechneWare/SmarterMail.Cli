using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Models
{
    public class IgnoredIp
    {
        public required string Ip { get; set; }
        public string? Description { get; set; }
        public required DateTime LastUpdated { get; set; }

        public bool IsSubnet => Ip.Contains('/');
    }
}
