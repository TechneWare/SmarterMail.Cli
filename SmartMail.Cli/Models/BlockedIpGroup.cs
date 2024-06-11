using NetTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Models
{
    /// <summary>
    /// Represents a CIDR group
    /// </summary>
    public class BlockedIpGroup
    {
        //The subnet expressed as a CIDR
        public string Subnet { get; set; } = "0.0.0.0/0";
        //Any IP addresses that are identified as being a part of this CIDR
        public List<BlockedIp> BlockedIps { get; set; } = [];
        //The range calculator to use for this group
        public IPAddressRange IpRange { get; set; }
        //Calculate the abuse % by taking the total number of IPs found in this subnet / the useable number of IPs in the CIDR's range
        public double PercentAbuse => NumFound / RangeSize;
        //How many blocked IPs were found in this subnet
        public double NumFound => (double)BlockedIps.Count;
        //The number of useable IPs in this subnet
        public double RangeSize => (double)IpRange!.AsEnumerable()
            .Where(i => !i.ToString().EndsWith(".0") && !i.ToString().EndsWith(".255")) //Don't include unuseable addresses
            .Count();

        //Gets the average score assigned to each of the member IPs
        public double AvgScore => BlockedIps.Any(i => i.Score > 0) ? 
            BlockedIps.Where(i => i.Score > 0).Average(i => i.Score) : 0;

        //Used to match to IPs when Virus Total data is not available
        public string SubnetBase
        {
            get
            {
                var sub = Subnet.Substring(0, Subnet.IndexOf('/') - 1);

                while (sub.Contains(".0"))
                    sub = sub.Replace(".0", "");

                return sub;
            }
        }

        public BlockedIpGroup(string subnet)
        {
            //Initilize the group with a range cacluator
            this.Subnet = subnet;
            this.BlockedIps = [];
            this.IpRange = IPAddressRange.Parse(subnet);
        }
    }
}
