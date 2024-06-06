# SmarterMail.Cli

## Overview
This project is a solution with two projects
- A REST client for the Smarter Mail Server API
- A simple commandline utility to manage an IP Black List on a SmarterMail server using the server's API

Both projects can be extended to provide more features and automation against a Smarter Mail server than smiply managing the blacklist. However, this initial version was created to solve my use case, which is automated managment the blacklist.

The commandline utility grew into something that can be extended to perform any action you want against a Smarter Mail server. 
With features such as:
- Auto login to the API and token maintenance
- Interactive or Commandline modes
- A scripting engine
- startup script
- Scheduled jobs
- Integration with Virus Total

The original use case, was to:
- Automatically move all incoming IDS blocks to the blacklist with consistent documentation
- Optimize the server's blacklist by identifying CIDR groups and collapsing IP entries

## Breif discussion on how this works

### Moving incoming IDS blocks to the blacklist
This is done as one might expect, by:
- Configuring the server's IDS rules to identify bad actor IP addresses, found under Settings > Scurity > IDS Rules.
	- _As I host a small footprint server that has only my own accounts and a few friends and family using it, I have opted for some fairly strict rules.  Your rules may need to be less restricitive depending on your users needs._
	- You can use a few of the IDS rules to act as honey pots and more quickly trap bad actor IP addresses as follows:
		- Since I have no POP users, I opted to:
			- Leave the POP ports open on the firewall and
			- Set the Denial of Service POP rule to flag any IP that attempts to use it once, with a long block time.
		- Since I know that all my users have valid passwords that currently work
			- Set the SMTP and IMAP Password Brute Force rule, to block after 1 failed attempt, with a long block time.
			- Set the SMTP Harvesting rule to block after 1 bad session, but with a shorter block time, but not shorter than the automation waits between executions.
	- Even if you don't configure extreemly strict rules, because you have many users that will forget their passwords; eventually you will find you have a very large list of IPs either showing up and falling off the IDS list after some time, or that you have moved manually to the permanent blacklist.
- Then remove IDS blocks and add/document them in the permanent blacklist

### Blacklist optimization
Optimizing the blacklist is done by examining all the known bad actor IPs, to see if any of them can be grouped together into a subnet or CIDR group. Identifying these CIDR groups allows replacing many IP address entries with a single entry that covers an entire subnet. If we consider the number of bad actor IPs found for a given subnet, then we can make a judgment call as to the reputation of the subnet, and thus choose when to implement a CIDR block that will encompas additional IP addresses.

CIDR blocking has two main benefits:
1. Since the subnet being blocked has a bad reputation:
	- Any furthar abuse from other IPs on that subnet will not generate new IDS or temporary blocks
	- Or incure any extra processing time by any protocol listener (EG: SMTP, POP, IMAP).
2. Since the blacklist is now shorter, it takes the server less time to search it when vetting future requests against the list.
	- If your hosting a very small footprint server, then reducing resource usage is especially important where possible. 
	- Even if you have a higher end server, as the list grows over time, it can eventually cause some noticable delay.

_See:_ [What is a CIDR](https://www.techtarget.com/searchnetworking/definition/CIDR).

### Identifying a CIDR group/subnet
If you have ever looked through the blacklist of an email server of any significant age, you will notice patterns in the IP addresses start to appear that can indicate that the owner of the subnet is likely not trustable on any IP address, and therefore the entire subnet can be blacklisted.

For Example, consider the following addresses:
```
80.244.11.65
80.244.11.118
80.244.11.119
80.244.11.143
80.244.11.147
80.244.11.244
```

It is easy to see the pattern here. The subnet `80.244.11.0/24` appears to be behaving poorly. In fact this is a real subnet that attacked my server 36 times while I was writing this.

By entering IPs such as `80.244.11.0/24` into your blacklist, you can effectivly block all the IPs on the `80.244.11.x` subnet with a single entry.  And we can feel fairly comfortable doing this as 36 out of 254 IPs or about 14.2% of the IPs on this subnet have been found to be bad actors. Thus this subnet could be considered to have a bad reputation and deserves a CIDR entry on the blacklist, shortening the list by 35 entries.

The `/24` represents the number of bits used in the subnet mask, and thus the portion of the IP address that contains the subnet.  In this case `255.255.255.0` or `11111111 11111111 11111111 00000000` in binary. And therefore we can see that the size of this subnet is the number of IP addresses that can fill out the last segment of the address. In this case 256(0->255), minus 1 for the broadcast address(255) and minus 1 for the 0 address(Legacy broadcast/modern ignore), which leaves you with 254 useable addresses on the subnet.

It can get more complicated however, since `/24` might not be the most accurate description of the 80.244.11.0 subnet.  
In this case it really is a /24, but consider this; what if the IP list looked more like this:
```
80.244.9.65
80.244.10.118
80.244.11.119
80.244.12.143
80.244.13.147
80.244.14.244
```

You might be tempted to use `80.244.0.0/16` as the CIDR group to block on. So how many IPs is this? Well roughly 65,025 possible addresses. Which would equate to approximatly 0.05% of the IPs found on this subnet are bad actors.  You might not feel comfortable blocking this entire address space. 

So the goal becomes, to identify subnets that are not trusted, while not penalizing subnets that have only a few bad actors on them.  Thus blocking traffic as minimally and optimally as possible. And at the same time automate this as much as possible, to get consistent documentation and reduce the amount manual interaction with the server to maintain the blacklist.  Plus it might be nice to be able to rebuild the blacklist or use the captured data in other systems/firewalls etc.

## The SmarterMail.CLI Utility
The SmarterMail.CLI utility was born out of the need to automate actions against a SmarterMail server's API and manage the servers blacklist.

Writing an API wrapper and then creating a hard-coded process to manage this list would be possible, but I chose to use the command pattern instead as exploring the API by hand was becoming tedious and I had to maintain access/refresh tokens, so it just made sense to make something that could deal with token managment for me, while I was free to explore the API.  

This then grew into the ability to auto login to the server on launch, script out one or more commands into a file and execute them and eventually to adjust the process using Virus Total data to get a more accurate identification of the CIDR group.

### Getting started

_The jargon that stuck here, is IPs on the permanent blacklist are refered to as a PermaBlocks and IPs on the IDS(Temprorary list) are refered to as TempBlocks._

1. Clone/Compile/Run this solution
	- At the prompt, type `settings` and press ENTER. Follow the prompts to configure the settings.
	- If this is the first run, then you will need at a minimum, to configure the Smarter Mail Server's address where the API endpoint can be found EG: `webmail.example.com`.
2. Pressing `ENTER` at the prompt will show a list of available commands
```
Usage: commandName [Arguments]
Commands:

Clear                             - Clears the screen
CommitProposed                    - Commits propposed changes from the current cache (loaded with load command)
DeleteBan [IpAddress/CIDR]        - Removes an IP/CIDR from the permanent blacklist
DeleteTemp [IpAddress]            - Removes an IP from the temporary block list
DMany [number]                    - Documents 1 or more undocumented perma blocked IPs
Doc [IpAddress protocol noload nosave]- Adds known IP Info to an IPs description in the servers settings>security>black list
IpInfo [IpAddress]                - Attempts to retreive IP Info from Virus Total for the specified IP Address
GetPermaBlockIps [show]           - loads permanetly blocked IP addresses to memory
GetTempIpBlockCounts              - returns the count of temporary IP blocks by service type
GetTempBlockIps                   - returns temporarily blocked IP addresses
Help [commandName]                - Displays help
Interactive                       - Launches the interactive shell
InvalidateCache                   - Invalidates the current cache, signaling other commands to reload it
Kill [JobId]                      - Kills the job with Id = JobId
Jobs                              - Displays any running jobs
LoadBlockedIpData                 - Loads all sources of blocked IP data into memory
Login [UserName Password]         - Logs into the API
MakeScript                        - Makes a commit script out of the proposed bans to be executed later
Print [message]                   - Prints a message to the output
Quit                              - Ends the program
RunScript [FullPathToScript]      - Executes a saved script EG: RunScript C:\temp\myscript.txt or run c:\temp\myscript.txt
SaveIpInfo                        - Saves Cached IP info from VirusTotal to file ipinfo.json
PermaBan [IP/CIDR Description]    - Adds or Updates an IP to the permanent black list
Sched [#Seconds commandToRun]     - Schedules a job to run on an interval until stopped
Session                           - Displays info about the current session
Settings                          - Configures settings
Version                           - Display Version Info
Wait [milliseconds]               - Waits the specified number of miliseconds
```

Most commands have a short version and you can get those by typeing `Help [CommandName]`

**_Note:_ You will need a server level admin account, a domain admin account is not sufficient**
- It is feesable that a normal user account could be used, but additional commands would need to be created that could manage that users mailbox/account. Currently, the only actions supported are based on the server level blacklist and thus only server level admin accounts are useful.

3. Type `Login [Username] [password]`
	- You should see the message `Logged In Successfully`. 
	- From this point forward, until you close the program, the client will remain connected to the server.

4. Type `LoadBlockedIpData` or just `load`, which will load the Count of IDS blocks, IDS list and the blacklist into memory.
	- At the same time it will generate a proposed blocking strategy
```
	[6/6/2024 12:58:20 AM UTC]Info      : ---- Loading IP block data ----
	[6/6/2024 12:58:20 AM UTC]Info      : Script loaded with 3 lines
	[6/6/2024 12:58:20 AM UTC]Info      : ---- Blocked IP Counts ----
	[6/6/2024 12:58:20 AM UTC]Info      :                 smtp:    0
	[6/6/2024 12:58:20 AM UTC]Info      :                 imap:    0
	[6/6/2024 12:58:20 AM UTC]Info      :                  pop:    0
	[6/6/2024 12:58:20 AM UTC]Info      :             delivery:    0
	[6/6/2024 12:58:20 AM UTC]Info      :                 ldap:    0
	[6/6/2024 12:58:20 AM UTC]Info      :      emailHarvesting:    0
	[6/6/2024 12:58:20 AM UTC]Info      :       greyListLegacy:    0
	[6/6/2024 12:58:20 AM UTC]Info      :                 xmpp:    0
	[6/6/2024 12:58:20 AM UTC]Info      :              webMail:    0
	[6/6/2024 12:58:20 AM UTC]Info      :           activeSync:    0
	[6/6/2024 12:58:20 AM UTC]Info      :              mapiEws:    0
	[6/6/2024 12:58:20 AM UTC]Info      :            authUsers:    0
	[6/6/2024 12:58:20 AM UTC]Info      :                Total:    0
	[6/6/2024 12:58:20 AM UTC]Info      : ---- Temp Blocked IPs ----
	[6/6/2024 12:58:20 AM UTC]Info      : Total Temp Blocks: 0
	[6/6/2024 12:58:20 AM UTC]Info      : Loading Perma Blocked IPs
	[6/6/2024 12:58:21 AM UTC]Info      : Total Perma Blocks: 888
	[6/6/2024 12:58:21 AM UTC]Info      : Found 0 Temp Blocks
	[6/6/2024 12:58:21 AM UTC]Info      : Found 888 Perma Blocks
	[6/6/2024 12:58:23 AM UTC]Info      : Total IPs:884     Existing Groups:4
	[6/6/2024 12:58:23 AM UTC]Info      : IPs added to existing groups:0
	[6/6/2024 12:58:23 AM UTC]Info      : Proposed: 0 IPs in 0 New Groups
	[6/6/2024 12:58:23 AM UTC]Info      : Proposed: Leave 884 perma blocks and Remove 0 perma blocks
	[6/6/2024 12:58:23 AM UTC]Info      : Proposed: Create 0 new Perma blocks
	[6/6/2024 12:58:23 AM UTC]Info      : -------------------------------
```
In this output you can see that there are currently no IDS blocks (Temp Blocked IPs) and that there are 888 Perma Blocked IPs and 4 CIDR groups on the blacklist. 

5. Type `Make` to generate the script that would implement the proposed blocking actions
	- You should see something like `Script saved to file: [path of executable]/commit_bans.txt`
Example Script:
```
# AUTO GENERATED SCRIPT TO COMMIT PROPOSED IP BANS - [6/1/2024 4:53:59 PM UTC]

# Perma ban the 80.255.11.0/24 subnet
pb 80.244.11.0/24 6/1/2024 4:53:59 PM| IPs[36] AvgScore[0.182] %Abuse[14.17%]
# Clean up existing perma bans
DeleteBan 80.244.11.57
DeleteBan 80.244.11.58
DeleteBan 80.244.11.60
DeleteBan 80.244.11.61
DeleteBan 80.244.11.62
DeleteBan 80.244.11.63
DeleteBan 80.244.11.64
DeleteBan 80.244.11.65
DeleteBan 80.244.11.66
#... 36 in total
DeleteBan 80.244.11.152

# Move any IDS blocks to the blacklist

# Delete temp ban for 1.231.115.252
dt 1.231.115.252
# Create a permaban with default documentation for 1.231.115.252
pb 1.231.115.252 6/1/2024 4:53:59 PM Default SMTP Password Brute Force strict rule

#... one for each IP in the IDS list
dt 220.174.209.154
pb 220.174.209.154 6/1/2024 4:53:59 PM Default SMTP Password Brute Force strict rule

# Reload the server data to memory
load
```

6. To execute the script type `run commit_bans.txt`


### Persisting settings
At the prompt enter `settings` and follow the prompts to create modify the settings or edit the config.json file

Example config.json:
```
{
  "VirusTotalApiKey": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "LoggingLevel": "Info",
  "Protocol": "https",
  "ServerAddress": "[API serverAddress/domainname]",
  "UseAutoTokenRefresh": true,
  "PercentAbuseTrigger": 0.005,
  "UseAutoLogin": true,
  "AutoLoginUsername": "[username]",
  "AutoLoginPassword": "[password]",
  "StartupScript": [
    "sched 70 docmany 4",
    "sched 600 make",
    "wait 5000",
    "sched 600 run commit_bans.txt",
    "load",
    "jobs"
  ]
}
```

This utility deals with this in two ways:
1. If using a Virus Total api key, then it uses the specific /xx CIDR group for each IP when grouping IPs together and determning the size of the subnet the IPs belong to.
2. If you are not using Virus Total, then it groups IPs into /24 and /16 subnets

In practice option 2, will group almost everything into /24 subnets, unless you have a lot of existing data that might trigger the creation of a /16 subnet.  Even using option 1(Virus Total data), its rare to get a CIDR block generated for a subnet larger than a /24.
See: [Virus Total](https://www.virustotal.com/)



Or if you don't trust it, its also possible to have it just generate the script to do the blocking and let you read it first.

The trick of course is to accuratly identify subnets, and thereby the subnet size and thus be able to calculate a percentage of abuse in a given subnet.
Currently there are two approaches used.
If relying only on Smarter Mail data, then IP subnet matching will only be against a /24 or /16 subnet
If using Virus Total data, then you get a more refined CIDR and thus can more accuratly calculate when to implement a CIDR block
Percent Abuse is the number of abusing IPs divided by the useable block size of a given subnet EG: %Abuse = badIPs / subnetSize
If you set a trigger threshold of .01, then it would want to block a /24 subnet when 3 or more bad IPs show up in that subnet or it would take 650 for a /16 subnet.
Thus the more accuratly you can get on the /xx of a subnet (CIDR), the better the scaling of the blocking trigger, 
and why I recommend using Virus Total for the added accuracy.


Credit goes out to @github/jsakamoto for creating a robust IP address range calculator.