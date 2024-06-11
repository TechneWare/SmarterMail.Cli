using NetTools;
using Newtonsoft.Json;
using SmartMailApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Models
{
    /// <summary>
    /// Holds all working data to be manipulated by one or more commands
    /// </summary>
    public static class Cache
    {
        // API Models
        public static bool IsLoaded { get; set; } = false;
        /// <summary>
        /// Holds the results of requesting IDS block counts from the server
        /// </summary>
        public static Dictionary<string, int> TempBlockCounts { get; set; } = [];
        /// <summary>
        /// Holds the result of requesting IDS blocks frm the server
        /// </summary>
        public static int TotalTempBlocks => TempBlockCounts != null ? TempBlockCounts.Sum(k => k.Value) : 0;
        /// <summary>
        /// Holds all known responses from the Virus Total api
        /// </summary>
        public static List<NetTools.VirusTotal.Models.IPAddressInfo> IPAddressInfos { get; set; } = [];

        //Local models to process with

        /// <summary>
        /// Holds all IDS block data from the server
        /// </summary>
        public static List<IpTempBlock> TempIpBlocks { get; set; } = [];
        /// <summary>
        /// Holds all black list data from the server
        /// </summary>
        public static List<IpAccess> PermaIpBlocks { get; set; } = [];

        //Local models to generate the proposed blocking strategy

        /// <summary>
        /// Holds a combined list of temporary and permanent blocks with data from best sources
        /// </summary>
        public static List<BlockedIp> AllBlockedIps { get; set; } = [];
        /// <summary>
        /// IPs that the system should keep out of the server's IDS and blacklist
        /// </summary>
        public static List<IgnoredIp> IgnoredIps { get; set; } = [];
        /// <summary>
        /// Holds a list of CIDR groups that currently exist
        /// </summary>
        public static List<BlockedIpGroup> BlockedIpGroups { get; set; } = [];
        /// <summary>
        /// Holds a list of CIDR groups that are proposed for the blocking strategy
        /// </summary>
        public static List<BlockedIpGroup> ProposedIpGroups { get; set; } = [];

        /// <summary>
        /// Resets the data used to calculate a blocking strategy
        /// </summary>
        public static void Clear()
        {
            AllBlockedIps.Clear();
            BlockedIpGroups.Clear();
            ProposedIpGroups.Clear();
        }
        /// <summary>
        /// Initilizes the cache from the SmarterMail server data
        /// </summary>
        /// <param name="tempBlockedIPs">Temporary IDS blocks</param>
        /// <param name="permaBlockedIps">Permanent Black List blocks</param>
        public static void Init(List<IpTempBlock> tempBlockedIPs, List<IpAccess> permaBlockedIps)
        {
            Clear();

            //Load the AllBlockedIps list
            foreach (var b in tempBlockedIPs)
            {
                Cache.AllBlockedIps.Add(new BlockedIp()
                {
                    Ip = b!.ip!,
                    IsTemporary = true,
                    BlockTimeRemaining = TimeSpan.FromSeconds(b.secondsLeftOnBlock),
                    Description = b.ruleDescription ?? b.protocol.ToString()
                });
            }

            //Load CIDR groups
            foreach (var b in permaBlockedIps)
            {
                if (b.IsSubnet)
                {
                    var newGroup = new BlockedIpGroup(b.ip!);
                    Cache.BlockedIpGroups.Add(newGroup);
                }
                else
                {
                    var newBlockedIp = new BlockedIp()
                    {
                        Ip = b!.ip!,
                        IsTemporary = false,
                        BlockTimeRemaining = null,
                        Description = b!.description ?? ""
                    };

                    Cache.AllBlockedIps.Add(newBlockedIp);
                }
            }

            //Build the proposed blocking strategy
            if (!string.IsNullOrEmpty(Globals.Settings.VirusTotalApiKey))
            {
                //More accurate way of getting subnet data
                BuildIpGroupsViaApi();
                BuildProposedIpGroupsViaApi();
            }
            else
            {
                //Educated guess at subnet data
                BuildIpGroups();
                BuildProposedIpGroups();
            }
        }
        public static void LoadIgnoreIps()
        {
            try
            {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
                if (File.Exists($"{path}/ipIgnore.json"))
                {
                    var ignores = File.ReadAllText($"{path}/ipIgnore.json");
                    IgnoredIps = JsonConvert.DeserializeObject<List<IgnoredIp>>(ignores) ?? [];
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error Loading ipIgnore.json Info: {ex.Message}");
            }
        }
        public static void SaveIgnoreIps()
        {
            try
            {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
                var ignores = JsonConvert.SerializeObject(IgnoredIps, Formatting.Indented);
                File.WriteAllText($"{path}/ipIgnore.json", ignores);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error Saving ipIgnore.json: {ex.Message}");
            }
        }
        public static void IgnoreIp(string ipAddress, string description)
        {
            IgnoredIp? ignoredIp = IgnoredIps.SingleOrDefault(i => i.Ip == ipAddress);
            if (ignoredIp == null)
            {
                ignoredIp = new IgnoredIp()
                {
                    Ip = ipAddress,
                    Description = description,
                    LastUpdated = DateTime.UtcNow
                };

                IgnoredIps.Add(ignoredIp);
                Globals.Logger.Info($"{ignoredIp.Ip} added to ignore list");
            }
            else
            {
                IgnoredIps.Remove(ignoredIp);
                Globals.Logger.Info($"{ignoredIp.Ip} removed from the ignore list");
            }

            SaveIgnoreIps();
        }

        /// <summary>
        /// Requests IP info from cache or from Virus Total, saving it to disk and updating the cache
        /// </summary>
        /// <param name="ipAddress">The IP Address to lookup</param>
        /// <returns>The response from the Virus Total api</returns>
        public static NetTools.VirusTotal.Models.IPAddressInfo? GetIpAddressInfo(string ipAddress, bool withSave)
        {
            //Perma Cache these objects - a TOODO could be to expire the objects based on last_analysis_date or last_modification_date
            //for the current use case however, its enough to have some info about the IP - since clearly anything in this list will be blocked anyway
            //This is used more for cataloging all discovered IPs and determining proper CIDR groups when optimizing the server black list
            //Possible enhancment would be to re-generate the entire SmarterMail server black list from this file
            var ipInfo = Models.Cache.IPAddressInfos.Where(inf => inf.id == ipAddress).SingleOrDefault();

            if (ipInfo == null)
            {
                //Data is not currently in the cache, so go get it
                var vtClient = new NetTools.VirusTotal.ApiClient();
                ipInfo = vtClient.GetIPAddressInfo(ipAddress).ConfigureAwait(false).GetAwaiter().GetResult();
                if (ipInfo != null)
                {
                    ipInfo.LastQuery = DateTime.UtcNow;
                    Models.Cache.IPAddressInfos.Add(ipInfo);
                    if (withSave)
                        Models.Cache.SaveIpInfoes();
                }
            }

            return ipInfo;
        }
        /// <summary>
        /// Saves the IP info(Virus Total Data) currently in the cache to ipinfo.json
        /// </summary>
        /// <exception cref="Exception">Bubbles the exception if there is a file system error</exception>
        public static void SaveIpInfoes()
        {
            try
            {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
                var infoes = JsonConvert.SerializeObject(IPAddressInfos, Formatting.Indented);
                File.WriteAllText($"{path}/ipinfo.json", infoes);

            }
            catch (Exception ex)
            {
                throw new Exception($"Error Saving ipinfo.json: {ex.Message}");
            }
        }
        /// <summary>
        /// Initilizes the cache with the stored Virus Total Data
        /// </summary>
        /// <exception cref="Exception">Bubbles the exception if there is a file system error</exception>
        public static void LoadIpInfoes()
        {
            try
            {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
                if (File.Exists($"{path}/ipinfo.json"))
                {
                    var infoes = File.ReadAllText($"{path}/ipinfo.json");
                    Cache.IPAddressInfos = JsonConvert.DeserializeObject<List<NetTools.VirusTotal.Models.IPAddressInfo>>(infoes) ?? [];
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error Loading ipinfo.json : {ex.Message}");
            }
        }
        /// <summary>
        /// Build the list of CIDR groups that are currently in the SmarterMail black list using Virus Total Data
        /// </summary>
        private static void BuildIpGroupsViaApi()
        {
            foreach (var g in Cache.BlockedIpGroups)
            {
                //Make a range cacluator for this subnet
                var ipR = IPAddressRange.Parse(g.Subnet);
                //Get all known IPs that are contined in the subnet
                var groupableIps = Cache.AllBlockedIps
                                .Where(i => !i.IsSubnet &&
                                ipR.Contains(IPAddress.Parse(i.Ip))).ToList();
                //Add anything that was found to the group
                foreach (var ip in groupableIps)
                    g.BlockedIps.Add(ip);
            }
        }
        /// <summary>
        /// Builds the list of proposed CIDR groups using Virus Total Data
        /// </summary>
        private static void BuildProposedIpGroupsViaApi()
        {
            //Find all IPs that are currently in a group
            var groupedIps = Cache.BlockedIpGroups
                .SelectMany(g => g.BlockedIps)
                .ToList();

            //Find all IPs that are candidates to be in a group
            var candidateIps = Cache.AllBlockedIps
                .Except(groupedIps)
                .ToList();

            //Make a list of CIDR groups from any known IPs that have Virus Total Data on them
            //By selecting a distinct list of subnets
            var allSubnets = Cache.AllBlockedIps
                .Where(i => i.IsDocumented)
                .Select(i => i.Subnet)
                .Distinct()
                .Select(subnet => new BlockedIpGroup(subnet))
                .ToList();

            //For each candidate IP
            while (candidateIps.Count != 0)
            {
                //Remove it from the candidates
                var ip = candidateIps.First();
                candidateIps.RemoveAt(0);

                //Find any CIDR groups that it should belong to
                var groupsForIp = allSubnets
                    .Where(n => n.IpRange.Contains(IPAddress.Parse(ip.Ip)))
                    .ToList();

                //For each group found, add the IP to it
                foreach (var g in groupsForIp)
                    g.BlockedIps.Add(ip);
            }

            //Set the list of proposed CIDR groups, where the group has IPs in it and that abuses more than the trigger amount
            var proposedGroups = allSubnets
                .Where(g => g.BlockedIps.Count > 1 && g.PercentAbuse > Globals.Settings.PercentAbuseTrigger)
                .ToList();

            //EG: If there are 254 useable addresses in the subnet and 3 IPs have been found to be abusive
            //Then the PercentAbuse is .012 > Trigger(.01) so add the group to the proposed CIDR list
            //The goal here is to scale up the minimum count of of abusive IPs found by the size of the CIDR range
            //before allowing a CIDR block to be generated on the server.
            //So CIDR/24 requires 3 IPs and CIDR/16 requires 651 IPs found before a block is generated

            Cache.ProposedIpGroups.AddRange(proposedGroups);
        }
        /// <summary>
        /// Build the list of CIDR groups that are currently in the SmarterMail black list
        /// </summary>
        private static void BuildIpGroups()
        {
            //Use a simple strategy to match IPs with CIDR groups
            var knownSubnets = Cache.BlockedIpGroups.Select(g => g.SubnetBase).ToList();
            var groupableIps = Cache.AllBlockedIps
                                    .Where(i => !i.IsSubnet &&
                                    knownSubnets.Any(s => i.Ip.StartsWith(s)));

            foreach (var ip in groupableIps)
            {
                var targetGroup = Cache.BlockedIpGroups.Where(g => ip.Ip.StartsWith(g.SubnetBase)).SingleOrDefault();
                targetGroup?.BlockedIps.Add(ip);
            }
        }
        /// <summary>
        /// Builds the list of proposed CIDR groups from SmarterMail server data
        /// </summary>
        private static void BuildProposedIpGroups()
        {
            var groupedIps = Cache.BlockedIpGroups.SelectMany(g => g.BlockedIps).ToList();
            var candidateIps = Cache.AllBlockedIps.Except(groupedIps).ToList();

            while (candidateIps.Any())
            {
                var ip = candidateIps.First();
                candidateIps.RemoveAt(0);

                //Get results if using 3,2 or 1 segments to match on
                var threeSegments = GetSegmentMatches(ip, 3, candidateIps);
                var twoSegments = GetSegmentMatches(ip, 2, candidateIps);
                var oneSegments = GetSegmentMatches(ip, 1, candidateIps);

                int threeSegThresh = 1;
                int twoSegThresh = 10;

                if ((threeSegments.Count >= threeSegThresh      //Threshold met and we have as many two as three segment matches
                 && threeSegments.Count <= twoSegments.Count    //              OR
                 && twoSegments.Count < twoSegThresh) ||        //The two and three segments are 100% match
                   (threeSegments.Count == twoSegments.Count    // but not yet met 2 segment threshold
                 && threeSegments.Count > 0))                   // and at least one match
                {
                    //Setup CIDR group for 3 segments (CIDR/24)
                    var threeSegGroup = MakeProposedGroup(candidateIps, ip, threeSegments, 3);

                    //If the group meets the abuse threshold, then propose it                    
                    if (threeSegGroup != null
                     && threeSegGroup.PercentAbuse > Globals.Settings.PercentAbuseTrigger)
                        Cache.ProposedIpGroups.Add(threeSegGroup);
                }
                else if ((twoSegments.Count >= twoSegThresh             //Threshold met and we have as many one as two segment matches
                       && oneSegments.Count >= twoSegments.Count) ||    //      OR
                         (twoSegments.Count == oneSegments.Count        //Two and One segments are 100%
                       && threeSegments.Count == 0                      //And nothing matched on 3 segments
                       && twoSegments.Count > twoSegThresh / 2))        //And at least the two segment threshold has been met
                {
                    //Setup CIDR group for 2 segments (CIDR/16)
                    var twoSegGroup = MakeProposedGroup(candidateIps, ip, twoSegments, 2);

                    //If the group meets the abuse threshold, then propose it
                    if (twoSegGroup != null
                     && twoSegGroup.PercentAbuse > Globals.Settings.PercentAbuseTrigger)
                        Cache.ProposedIpGroups.Add(twoSegGroup);
                }
            }
        }
        /// <summary>
        /// Generates a proposed group from an IP and any matches to that IP over a number of segments
        /// </summary>
        /// <param name="candidateIps">List of IPs to clean up</param>
        /// <param name="ip">The IP to consider</param>
        /// <param name="segmentIps">The list of IPs that matched this IP over a number of segments</param>
        /// <param name="segmentCount">The number of segments that were considered in the match</param>
        /// <returns>A configured CIDR group</returns>
        private static BlockedIpGroup? MakeProposedGroup(List<BlockedIp> candidateIps, BlockedIp ip, List<BlockedIp> segmentIps, int segmentCount)
        {
            BlockedIpGroup? result = null;
            var mySubnet = GetIpSubnet(ip.Ip, segmentCount);
            var existingGroup = Cache.BlockedIpGroups.Where(g => g.SubnetBase == mySubnet).FirstOrDefault();
            if (existingGroup != null)
            {
                if (!existingGroup.BlockedIps.Contains(ip))
                    existingGroup.BlockedIps.Add(ip);

                existingGroup.BlockedIps.AddRange(segmentIps);

                Cache.ProposedIpGroups.Add(existingGroup);
            }
            else
            {
                var CIDR = GetCIDR(mySubnet, segmentCount);
                var newGroup = new BlockedIpGroup(CIDR);
                newGroup.BlockedIps.Add(ip);
                newGroup.BlockedIps.AddRange(segmentIps);

                result = newGroup;
            }

            candidateIps.RemoveAll(i => segmentIps.Contains(i));

            return result;
        }
        /// <summary>
        /// Finds all IPs that match on a number of segments
        /// </summary>
        /// <param name="ip">The IP address to consider</param>
        /// <param name="numSegments">The number of segments to match on</param>
        /// <param name="searchList">The list of IPs to search</param>
        /// <returns>Any IPs that match over a number of segments</returns>
        private static List<BlockedIp> GetSegmentMatches(BlockedIp ip, int numSegments, List<BlockedIp> searchList)
        {
            var mySegments = GetIpSubnet(ip.Ip, numSegments);
            var matchingIps = searchList.Where(i => i.Ip.StartsWith(mySegments)).ToList();

            var subnet = GetCIDR(mySegments, numSegments);

            var rng = IPAddressRange.Parse(subnet);
            var matched = searchList.Where(i => rng.Contains(IPAddress.Parse(i.Ip))).ToList();

            return matchingIps;
        }
        /// <summary>
        /// Generates a CIDR range from an IP address for 1-3 segments
        /// </summary>
        /// <param name="ip">IP to extract the CIDR from</param>
        /// <param name="numSegments">The number of segments to use for the subnet</param>
        /// <returns>CIDR of the IP based on segments</returns>
        /// <exception cref="Exception">numSegments must be between 1 and 3</exception>
        private static string GetCIDR(string ip, int numSegments)
        {
            var subnet = $"{ip}0".Replace("00", "0");
            while (subnet.ToCharArray().Select(c => c == '.').Count() < 3)
                subnet = $"{subnet}.0";

            if (numSegments == 3)
                subnet = $"{subnet}/24";
            else if (numSegments == 2)
                subnet = $"{subnet}/16";
            else if (numSegments == 1)
                subnet = $"{subnet}/8";
            else
                throw new Exception("numSegments must be between 1 and 3");

            return subnet;
        }
        /// <summary>
        /// Gets the subnet if using a number of segments
        /// </summary>
        /// <param name="ip">ip to extract subnet from</param>
        /// <param name="numSegments">number of segments to consider in the subnet</param>
        /// <returns>The subnet portion of the IP address</returns>
        private static string GetIpSubnet(string ip, int numSegments)
        {
            var subnet = string.Empty;
            int found = 0;
            int idx = 0;

            while (found < numSegments && idx < ip.Length - 1)
            {
                if (ip[idx] == '.')
                {
                    found++;
                    subnet = ip.Substring(0, idx);
                }

                idx++;
            }

            return $"{subnet}.";
        }
        /// <summary>
        /// Converts a Unix Timestamp to a DateTime object
        /// </summary>
        /// <param name="unixTimeStamp">Long Int value of the Unix Timestamp</param>
        /// <returns></returns>
        public static DateTime UnixTimeToDateTimeUTC(double unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(unixTimeStamp).ToUniversalTime();
        }
    }
}
