# SmarterMail.Cli

## Overview
This is a solution with two projects
- A REST client for the Smarter Mail Server API
- A simple command line utility to manage an IP Black List on a SmarterMail server using the server's API

Both projects have the potential to be extended and provide more features and automation against a Smarter Mail server than simply managing the block list. However, I created this initial version to solve my use case: automated blocklist management.
The command line utility grew into something that can be extended to perform any action against a Smarter Mail server. 

With features such as:
- Auto-login to the API and token maintenance
- Interactive or Commandline modes
- A scripting engine
- Startup script
- Scheduled jobs
- Integration with Virus Total

The original use case was to:
- Automatically move all incoming IDS blocks to the blocklist with consistent documentation.
- Optimize the server's blocklist by identifying CIDR groups and collapsing IP entries.

## A brief discussion on how this works

### Moving incoming IDS blocks to the blocklist
This is done as one might expect, by first:
- Configuring the server's IDS rules to identify bad actor IP addresses is found under Settings > Scurity > IDS Rules.
	- _As I host a small footprint server with only my accounts and a few friends and family using it, I have opted for some pretty strict rules. Depending on your user's needs, your rules may need to be less restrictive._
	- You can use a few of the IDS rules to act as honey pots and more quickly trap bad actor IP addresses as follows:
		- Since I have no POP users, I opted to:
			- Leave the POP ports open on the firewall and
			- Set the Denial of Service POP rule to flag any IP that attempts to use it once, with a long block time.
		- Since I know that all my users have valid passwords that currently work
			- Set the SMTP and IMAP Password Brute Force rule to block after one failed attempt, with a long block time.
			- Set the SMTP Harvesting rule to block after 1 bad session, but with a shorter block time—not shorter than the automation waits between executions.
	- Even if you don't configure stringent rules because many users will forget their passwords, eventually, you will find you have an extensive list of IPs either showing up and falling off the IDS list after some time or that you have manually moved to the permanent blocklist.
- Then, remove each IDS block and add/document it in the permanent blocklist.

### Blocklist optimization
Optimizing the blocklist is done by examining all the known bad actor IPs to see if any of them can be grouped into a subnet or CIDR group. Identifying these CIDR groups allows for replacing many IP address entries with a single entry covering an entire subnet. Consider the number of bad actor IPs found for a given subnet. We can make a judgment call as to the reputation of the subnet and thus choose when to implement a CIDR block that will encompass additional IP addresses.

CIDR blocking has two main benefits:
1. Since the subnet being blocked has a bad reputation:
	- Any further abuse from other IPs on that subnet will not generate new IDS or temporary blocks.
	- Or incur any extra processing time by any protocol listener (EG: SMTP, POP, IMAP).
2. Since the blocklist is now shorter, it takes the server less time to search it when vetting future requests against the list.
	- Reducing resource usage is especially important if you're hosting a minimal-footprint server.
	- Even if you have a higher-end server, as the list grows over time, it can eventually cause some noticeable delay.

_See:_ [What is a CIDR](https://www.techtarget.com/searchnetworking/definition/CIDR)?

### Identifying a CIDR group/subnet
Suppose you have ever looked through the blocklist of an email server of any significant age. In that case, you will notice patterns in the IP addresses that start to appear that indicate that the subnet's owner is likely not trustworthy on any IP address. Therefore, the entire subnet can be blocked.

For Example, consider the following addresses:
```
80.244.11.65
80.244.11.118
80.244.11.119
80.244.11.143
80.244.11.147
80.244.11.244
```

It is easy to see the pattern here. The subnet `80.244.11.0/24` appears to be behaving poorly. This is an actual subnet that attacked my server 36 times while I was writing this.

By entering IPs such as `80.244.11.0/24` into your blocklist, you can effectively block all the IPs on the `80.244.11.x` subnet with a single entry. We can feel pretty comfortable doing this, as 36 out of 254 IPs, or about 14.2% of the IPs on this subnet, have been found to be bad actors. Thus, this subnet could be considered to have a bad reputation and deserves a CIDR entry on the blocklist, shortening the list by 35 entries.

The `/24` represents the number of bits used in the subnet mask and, thus, the portion of the IP address that contains the subnet. In this case `255.255.255.0` or `11111111 11111111 11111111 00000000` in binary. Therefore we can see that the size of this subnet is the number of IP addresses that can fill out the last segment of the address. In this case 256(0->255), minus 1 for the broadcast address(255) and minus 1 for the 0 address(Legacy broadcast/modern ignore), which leaves you with 254 useable addresses on the subnet.
However, it can get more complicated since /24 might not be the most accurate description of the 80.244.11.0 subnet.

Consider this; what if the IP list looked more like this:
```
80.244.9.65
80.244.10.118
80.244.11.119
80.244.12.143
80.244.13.147
80.244.14.244
```

You might be tempted to use `80.244.0.0/16` as the CIDR group to block on. So, how many IPs is this? Well, roughly 65,025 possible addresses, which would equate to approximately 0.05% of the IPs found on this subnet being bad actors. You might not feel comfortable blocking this entire address space.

So, the goal is to identify subnets that are not trusted while not penalizing subnets with only a few bad actors in them and blocking traffic as minimally and optimally as possible. At the same time, automate this as much as possible to get consistent documentation and reduce the amount of manual interaction with the server to maintain the blocklist. Plus, it might be nice to rebuild the blocklist or use the captured data in other systems/firewalls, etc.

## The SmarterMail.CLI Utility
The SmarterMail.CLI utility was created to automate actions against a SmarterMail server's API and manage the server's blocklist.

Writing an API wrapper and then creating a hard-coded process to manage this list would be possible, but I chose to use the command pattern instead. Exploring the API by hand was becoming tedious, and I had to maintain access/refresh tokens, so it just made sense to make something that could deal with token management for me while I was free to explore the API.

This then grew into the ability to auto-log into the server on launch, Script one or more commands into a file, execute them, and eventually adjust the process using Virus Total data to identify the CIDR groups accurately.

### Getting started

_The jargon evolved here: IPs on the permanent blocklist are called PermaBlocks, and IPs on the IDS(Temporary list) are called TempBlocks._

1. Clone/Compile/Run the solution
	- At the prompt, type `settings` and press ENTER. Follow the prompts to configure the settings.
	- If this is the first run, you will need to configure the Smarter Mail Server's address where the API endpoint can be found, e.g., `webmail.example.com`.
2. Pressing `ENTER` at the prompt will show a list of available commands
```
Usage: commandName [Arguments]
Commands:

Clear                             - Clears the screen
CommitProposed                    - Commits propposed changes from the current cache (loaded with load command)
DeleteBan [IpAddress/CIDR]        - Removes an IP/CIDR from the permanent blocklist
DeleteTemp [IpAddress]            - Removes an IP from the temporary blocklist
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

Most commands have a short version, and you can get those by typing `Help [CommandName]`

**_Note:_ You will need a server-level admin account; a domain admin account is not sufficient**
- A normal user account could be used, but additional commands would need to be created to manage that user's mailbox/account. Currently, the only actions supported are based on the server-level blocklist; thus, only server-level admin accounts are useful.

3. Type `Login [Username] [password]`
	- You should see the message `Logged In Successfully`. 
	- From this point forward, until you close the program, the client will remain connected to the server.

4. Type `LoadBlockedIpData` or just `load`, which will load the Count of IDS blocks, IDS list and the blocklist into memory.
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
This output shows that there are currently no IDS blocks (Temp Blocked IPs), 888 Perma Blocked IPs, and 4 CIDR groups on the blocklist.

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

# Move any IDS blocks to the blocklist

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


### Persisting settings and configuring a startup script
At the prompt enter `settings` and follow the prompts to modify the settings or edit the config.json file

Example config.json:
```json
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

- **VirusTotalApiKey:** (Optional) api key to your free or better Virus Total Account
	- See: [Virus Total](https://www.virustotal.com/)
	- Virus Total allows more accurate identification of CIDR ranges
	- Without an api key, IPs are categorized into /24 and /16 networks only
- **LoggingLevel:** Valid values (Debug, Info, Warning, Error)
- **Protocol:** http or https
	- The http protocol to use when communicating with the Smarter Mail api
- **UseAutoTokenRefresh:** true or false
	- If true, the client will maintain an active connection
	- If false, the API wrapper (SmartMailApiClient) will refresh the token as needed upon the next request
- **PercentAbuseTrigger:** A number between 0 and 1, to trigger CIDR blocking, if the subnet exceeds this percentage of bad actor IPs.
	- The settings command limits this to .005 to 1, as .005 in a /24 subnet requires at least 2 IPs to trigger blocking.
- **UseAutoLogin:** If true will attempt to use the AutoLoginUsername and AutoLoginPassword to connect to the server at launch.
- **AutoLoginUsername:** The account to login with
- **AutoLoginPassword:** The password to the account
- **StartupScript:** An array of strings, where each element represents a single line in a startup script
	- Can be used to configure the CLI to run a set of commands and schedule jobs on startup.

config.json fragment:
```json
"StartupScript": [
    "sched 70 docmany 4",
    "sched 600 make",
    "wait 5000",
    "sched 600 run commit_bans.txt",
    "load",
    "jobs"
  ]
```
The above startup script does the following:
- Schedules the `docmany` command to run every 70 seconds, and document up to `4` IPs on the block list using Virus Total data
- Schedules the `make` command to run every 10 minutes, generating the `commit_bans.txt` script
- `Wait` 5 seconds so that the `run` command will always run 5 seconds after the `make` command
- Schedules the `run` command to run every 10 minutes and execute the script `commit_bans.txt`
- Loads the server data into memory
- Displays scheduled jobs

### About Virus Total
Currently, the CLI is hard-coded to the free account quota limits. However, if you're paying for a Virus Total account and have higher quota limits, you have the power to modify the Globals.cs file to set the limits as you would like. This flexibility allows you to tailor the usage to your specific needs. I'll probably refactor this and make it configurable soon, but right now, that's how it works.

Quota limits are set in the Globals.cs file:
```csharp
public static int vtMinuteQuota { get; set; } = 4;
public static int vtDailyQuota { get; set; } = 500;
```

If you launch with the above script, there may be a period where it's collecting data on all the IPs in your block list from Virus Total. The free account gives you 4 hits/minute and 500 hits/day, so it's possible you may go over this initially. That's okay because a built-in quota tracking system prevents further hits against the API when you're over your quota. This system ensures that your usage is always within the set limits. When the quota period expires, it will continue documenting IPs without any intervention required. Currently its hard coded to use the free account values, but if you modify the Globals.cs file, you can set your own quota limits.

Unfortunately, Virus Total does not give free accounts access to the endpoint that gives quota information, and they don't return quota information in the headers. There is also a 15.5k hits/month quota, but I did not implement that. Hopefully, I won't need to. In any case, if an API request detects that the quota was reached, it will not allow further hits until the next day. The day quota expires at midnight UTC and the month quota will reset at midnight UTC on the 1st of the month.

### Is it working?
Let it run for a few days if needed or forever. You should notice that your IDS list remains primarily empty, only containing IPs that were last detected within 10 minutes, while your block list will continue to grow.  

IP descriptions on the block list are initially set to the basic information available from the IDS block, EG: What protocol, country etc. the IP address was found using. Every time `docmany 4` executes, it will look for up to 4 undocumented IPs in the block list, query Virus Total, and refresh their descriptions. Future lookups against an IP will come from that file instead of using up a hit on the Virus Total API.

Once the IP has been documented, it will have an accurate CIDR block assigned to it and can now be considered by the `make` command for inclusion in the commit_bans.txt script file.

You can see that its working, if you examine the logs for POP, IMAP or SMTP.  SMTP is likely the most active.

Sample SMTP log for a single blocked request:
```
15:24:36.585 [80.244.11.121][60044376] connected at 6/6/2024 3:24:36 PM
15:24:36.585 [80.244.11.121][60044376] "421 Server is busy, try again later." response returned.
15:24:36.585 [80.244.11.121][60044376] IP is blacklisted
15:24:36.585 [80.244.11.121][60044376] disconnected at 6/6/2024 3:24:36 PM
```
If you see this going on, then it's working.

## Commands
All commands can be used in a script, used in interactive mode or used from the commandline unless otherwise noted.

### Clear

| Command Aliases | Comments|
| :--- | :--- |
| `clear` `cls` | Clears the screen, can be used from command line, but mostly used for interactive mode.|

### CommitProposed

| Command Alias | Comments |
| :--- | :--- |
| `CommitProposed` `cp` | Commits propposed changes from the current cache (loaded with load command). Cleans up the IDs blocks and condenses all perma blocks into CIDR ranges where possible. |

_Note: This command does not use the script engine, it will immediatly execute the plan stored in memory. It is recommended to use the Make/Run commands instead_

### DeleteBan

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `DeleteBan` `db` | IpAddress/CIDR | Removes an IP/CIDR from the permanent blacklist |

### DeleteTemp

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `DeleteTemp` `dt` | IpAddress | Removes an IP from the temporary block list or IDS blocks |

### DMany

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `DMany` `dm` `docmany` | number | Documents 1 or more undocumented perma blocked IPs |

_Note: This command requires a working Virus Total api key_
_For free accounts, it is recommended not to use more then `4` for the `number` parameter, as 4 hits/min is the quota_

### Doc

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Doc` | IpAddress protocol noload nosave | Documents an IP using Virus Total Data by setting the description in the servers settings>security>black list |

- **IpAddress:** An IP address that is currently listed in the block list the server under settings>security>black list
- **protocol:** The protocol that was used when the IP was banned. EG: SMTP, POP, IMAP
- **noload:** Prevents reloading of the cache after execution. Useful in scripts where many doc command are issued.  Issue a `load` command to reload the cache later.
- **nosave:** Prevents saving of Virus Total data to the IpInfo.json file after execution. Useful in scripts where many doc command are issued.  Issue a `SaveIpInfo` command to reload the cache later.

### IpInfo

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `IpInfo` `ip` | IpAddress | Attempts to retreive IP Info from Virus Total for the specified IP Address |

_Display only - will save the returned data to IpInfo.json_

### GetPermaBlockIps

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `GetPermaBlockIps` `gpbip` `gpb` | show | loads permanetly blocked IP addresses to memory |

_If `show` is included, will also display the loaded data_

### GetTempIpBlockCounts

| Command Alias | Comments |
| :--- | :--- |
| `GetTempIpBlockCounts` `gtbc` `tbc` | displays the count of temporary IP blocks by service type |

### GetTempBlockIps

| Command Alias | Comments |
| :--- | :--- |
| `GetTempBlockIps` `gtbip` `tb` | loads temporarily blocked IP addresses to memory and displays them |

### Help

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Help` `h` | commandName | Displays help on the specified command |

### Interactive

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Interactive` `shell` | args | Launches an interactive shell |

_Used to interact with a user_

- **args:** Any arguments you want to pass to the shell, same as executing from the command line

### InvalidateCache

| Command Alias | Comments |
| :--- | :--- |
| `InvalidateCache` | Invalidates the current cache, signaling other commands to reload it |

_Used in scripts to make sure the cache appears invalid for follow on commands_

### Kill

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `kill` | JobId | Kills the job with Id = JobId |

_See: `Sched` to schedule a job_

### Jobs

| Command Alias | Comments |
| :--- | :--- |
| `Jobs` | Displays any running jobs |

_Use this command to get the JobId for a job_
_See: `Sched` to schedule a job_

### LoadBlockedIpData

| Command Alias | Comments |
| :--- | :--- |
| `LoadBlockedIpData` `load` | Loads all sources of blocked IP data into memory and generates a blocking plan |

_Data is anyalized with find subnets to identify groups of malicious IPs_

### Login

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Login` | Username Password | Logs into the API, retreiving auth tokens for the current session |

_Refresh token tracking starts upon successful login_

### MakeScript

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `MakeScript` `make` | Makes a commit script out of the proposed bans to be executed later |

_Script is saved to the file `commit_bans.txt` in the directory of the executable_

### Print

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Print` | message | Prints a message to the output

_Useful in scripts_

### Quit

| Command Alias | Comments |
| :--- | :--- |
| `Quit` `exit` | End the application if in interactive mode |

### RunScript

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `RunScript` `run` `exe` | FullPathToScript | Executes a saved script EG: RunScript C:\temp\myscript.txt or run c:\temp\myscript.txt |

### SaveIpInfo

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `SaveIpInfo` | Saves Cached IP info from Virus Total to file `ipinfo.json` |

### PermaBan

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `PermaBan` `pb` | IP/CIDR Description | Adds or Updates an IP to the permanent block list |

_EG:_
_pb 192.168.1.1 I banned this IP_
_pb 192.168.1.0/24 I banned this CIDR_

### Sched

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Sched` | #Seconds commandToRun | Schedules a job to run on an interval until stopped |

_EG: To run a script every 30 seconds use: `sched 30 run c:\temp\myscript.txt`_

### Session

| Command Alias | Comments |
| :--- | :--- |
| `Session` | Displays info about the current session |

_This displays information about the currently logged on user_

### Settings

| Command Alias | Comments |
| :--- | :--- |
| `Settings` `set` | Configures settings |

_Settings are saved to `config.json`_

### Version

| Command Alias | Comments |
| :--- | :--- |
| `Version` `ver` `v` | Display Version/Current build Info |

### Wait

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Wait` | milliseconds | Waits the specified number of miliseconds |

_Useful in scripts_
_EG: to set the timing between two jobs with the same period, introduce a delay between the `sched` commands_

Credit to: @github/jsakamoto for creating a robust IP address range calculator (https://github.com/jsakamoto/ipaddressrange/).
