# SmarterMail.Cli

## Overview
This is a solution with two projects
- A REST client for the Smarter Mail Server API
- A simple command line utility to manage an IP Black List on a SmarterMail server using the server's API

Both projects have the potential to be extended and provide more features and automation against a Smarter Mail server than simply managing the blocklist. However, I created this initial version to solve my use case: automated blocklist management.

See: [Initial Results](https://github.com/TechneWare/SmarterMail.Cli/discussions/1)

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
- Configuring the server's IDS rules to identify bad actor IP addresses: 
	- found on the server under `Settings > Security > IDS Rules`.
- Then, remove each IDS block and add/document it in the permanent blocklist.

### Notes on Server Config
You must configure the server's IDS rules in order to flag incoming IPs as abusive. See: 
- [SmarterMail IDS Rules](https://help.smartertools.com/smartermail/current/topics/systemadmin/security/advanced/abusedetection)
- [SmarterMail IDS Blocks](https://help.smartertools.com/smartermail/current/topics/systemadmin/manage/currentblocks)

_**Note:** As I host a small footprint server with only my accounts and a few friends and family using it, I have opted for some pretty strict rules. Depending on your user's needs, your rules may need to be less restrictive. This is just how I configured my IDS rules._

**Using IDS Rules as a honey pot:**
- You can use a few of the IDS rules to act as honey pots and more quickly trap bad actor IP addresses as follows:
	- Since I have no POP users, I opted to:
		- Leave the POP ports open on the firewall and
		- Set the Denial of Service POP rule to flag any IP that attempts to use it once, with a long block time.
	- Since I know that all my users have valid passwords that currently work, and they will talk to me directly if there is an issue, I opted to:
		- Set the SMTP and IMAP Password Brute Force rules to block after one failed attempt, with a long block time.
		- Set the SMTP harvesting rule to block after 1 bad session, but with a shorter block time, which is not shorter than the automation waits between executions.
	
### Is this for me?
Even if you don't configure stringent rules because many users will forget their passwords, eventually, you will find you have an extensive list of IPs either showing up and falling off the IDS list after some time or that you have manually moved to the permanent blocklist.

- If you find that you don't want/have time to constantly monitor the IDS blocks and move them to the permanent blocklist.
- Or you would just like to optimize your block list.
- Or you are interested in a foundational starting point to access other parts of the Smarter Mail API using your own code.
	- See API documentation from the server at:
		- `Settings > API Documentation`
		- Or `https://[your servers web address]/Documentation/api#/topics/overview`
- Then this utility/project may be for you.

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
```logos
80.244.11.65
80.244.11.118
80.244.11.119
80.244.11.143
80.244.11.147
80.244.11.244
```

It is easy to see the pattern here. The subnet `80.244.11.0/24` appears to be behaving poorly. This is an actual subnet that attacked my server 36 times while I was writing this.

By entering IPs such as `80.244.11.0/24` into your blocklist, you can effectively block all the IPs on the `80.244.11.x` subnet with a single entry. We can feel pretty comfortable doing this, as 36 out of 254 IPs, or about 14.2% of the IPs on this subnet, have been found to be bad actors. Thus, this subnet could be considered to have a bad reputation and deserves a CIDR entry on the blocklist, shortening the list by 35 entries.

The `/24` represents the number of bits used in the subnet mask and, thus, the portion of the IP address that contains the subnet. In this case, `255.255.255.0` or `11111111 11111111 11111111 00000000` in binary. Therefore, we can see that the size of this subnet is the number of IP addresses that can fill out the last segment of the address. In this case, 256(0->255), minus 1 for the broadcast address(255), and minus 1 for the 0 address(Legacy broadcast/modern ignore), which leaves you with 254 useable addresses on the subnet.

However, it can get more complicated since /24 might not accurately describe the 80.244.11.0 subnet.

Consider this: what if the IP list looked more like:
```logos
80.244.9.65
80.244.10.118
80.244.11.119
80.244.12.143
80.244.13.147
80.244.14.244
```

You might be tempted to use `80.244.0.0/16` as the CIDR group to block on. So, how many IPs is this? Well, roughly 65,534 possible addresses, which would equate to approximately 0.05% of the IPs found on this subnet being bad actors. You might not feel comfortable blocking this entire address space, even if you found 36 abusive IPs coming from this subnet.

If we can accuratly identify the CIDR range for a given IP, then we can more accuratly judge how abusive a subnet is and scale up how many IP discoveries it takes to issue a block for a given CIDR range. 

For example suppose we choose to block at 0.5% abuse
- Subnet `Size` * `0.5%` = `Min#Abusive_IPs` to trigger a CIDR block

| CIDR | Size | #IPs |
| :---: | :---: | :---: |
| /24 | 254 | 2 |
| /21 | 2046 | 11 |
| /18 | 16382 | 82 |
| /16 | 65534 | 328 |

As you can see, this allows the trigger value to scale up with CIDR size.  This is why a Virus Total API key is recommended, as not having that falls back to only using /24 or /16 CIDR ranges.

So, the goal is to identify subnets that are not trusted while not penalizing subnets with only a few bad actors in them and blocking traffic as minimally and optimally as possible. At the same time, automate this as much as possible to get consistent documentation and reduce the amount of manual interaction with the server to maintain the blocklist. Plus, it might be nice to rebuild the blocklist or use the captured data in other systems/firewalls, etc.

## The SmarterMail.CLI Utility
The SmarterMail.CLI utility automates actions against a SmarterMail server's API and manages the server's blocklist.

Writing an API wrapper and creating a hard-coded process to manage this list would be possible, but I used the command pattern instead. Exploring the API by hand was becoming tedious, and I had to maintain access/refresh tokens, so it just made sense to make something that could manage tokens for me while I was free to explore the API.

This then grew into the ability to auto-log into the server on launch, Script one or more commands into a file, execute them, and eventually adjust the process using Virus Total data to identify the CIDR groups accurately.

### Getting started

_The jargon evolved here: IPs on the permanent blocklist are called PermaBlocks, and IPs on the IDS(Temporary list) are called TempBlocks._

1. Clone/Compile/Run the solution
 	- If you want to build this on a Raspberry PI
  		- Make sure you have .net 8 installed.
    	- A good article can be found here: [Install and use Microsoft Dot NET 8 with the Raspberry Pi](https://www.petecodes.co.uk/install-and-use-microsoft-dot-net-8-with-the-raspberry-pi/)
  	- Navigate to the root folder where the file `SmartMail.Cli.sln` is found:
		- For 64bit Arm: `dotnet publish --runtime linux-arm64 --self-contained` 
		- For 32bit Arm: `dotnet publish --runtime linux-arm --self-contained`
  			- add `-o [output folder]` to redirect to your desired output folder
      		I tried using 64bit Arm on a Raspberry PI 4b, but I believe there is a bug with 64bit compiling at this time, so I ended up using the 32bit version. It could also be that my Raspberry PI has had its OS upgraded in-place several times, and so, even though the OS reports as 64bit, it only wants to work with 32bit compiled ARM. Try both ways if you're curious.
  		- Switch folders to `/bin/Release/net8.0/linux-arm/` or the specified output folder, and execute with `./SmartMail.Cli`
    		- The process should be similar for other Linux distros; provide the proper runtime `--runtime [runtime for your platform]`.
         		- See: [.NET RID Catalog](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog) for your particular platform.
	- At the prompt, type `settings` and press ENTER. Follow the prompts to configure the settings.
		- If this is the first run, you must configure the Smarter Mail Server's address where the API endpoint can be found, e.g., `webmail.example.com`.
	
3. Pressing `ENTER` at the prompt will show a list of available commands
```fancy
Usage: commandName [Arguments]
Commands:

Clear                             - Clears the screen
CommitProposed                    - Commits proposed changes from the current cache (loaded with load command)
DeleteBan [IpAddress/CIDR]        - Removes an IP/CIDR from the permanent blacklist
DeleteTemp [IpAddress]            - Removes an IP from the temporary block list
DMany [number]                    - Documents 1 or more undocumented perma blocked IPs
Doc [IpAddress protocol noload nosave]- Adds known IP Info to an IPs description in the servers settings>security>black list
IpInfo [IpAddress]                - Attempts to retrieve IP Info from Virus Total for the specified IP Address
GetPermaBlockIps [show]           - loads permanently blocked IP addresses to memory
GetTempIpBlockCounts              - returns the count of temporary IP blocks by service type
GetTempBlockIps                   - returns temporarily blocked IP addresses
Help [commandName]                - Displays help
Ignore [Ip/CIDR Description]      - Toggles the Ignore status of an IP or CIDR address
Interactive                       - Launches the interactive shell
InvalidateCache                   - Invalidates the current cache, signaling other commands to reload it
Kill [JobId]                      - Kills the job with Id = JobId
Jobs                              - Displays any running jobs
LoadBlockedIpData                 - Loads all sources of blocked IP data into memory
Login [UserName Password]         - Logs into the API
MakeScript                        - Makes a commit script out of the proposed bans to be executed later
Print [message]                   - Prints a message to the output
Quit                              - Ends the program
Restore [IpFragment | * force]    - Restores the black list from previously stored data.
RunScript [FullPathToScript]      - Executes a saved script EG: RunScript C:\temp\myscript.txt or run c:\temp\myscript.txt
SaveIpInfo                        - Saves Cached IP info from VirusTotal to file ipinfo.json
PermaBan [IP/CIDR Description]    - Adds or Updates an IP to the permanent black list
Sched [#Seconds commandToRun]     - Schedules a job to run on an interval until stopped
Session                           - Displays info about the current session
SetOption [option value]          - Sets a system option to the specified value
Settings                          - Configures settings
UpdateBlacklist                   - Builds a blocking script and executes it in one step
Version                           - Display Version Info
Wait [milliseconds]               - Waits the specified number of milliseconds
```

Most commands have a short version, and you can get those by typing `Help [CommandName]`

**_Note:_ You will need a server-level admin account; a domain admin account is not sufficient**
- A normal user account could be used, but additional commands would need to be created to manage that user's mailbox/account. Currently, the only actions supported are based on the server-level blocklist; thus, only server-level admin accounts are useful.

3. Type `Login [Username] [password]`
	- You should see the message `Logged In Successfully`. 
	- From this point forward, until you close the program, the client will remain connected to the server.

4. Type `LoadBlockedIpData` or just `load`, which will load the Count of IDS blocks, IDS list and the blocklist into memory.
	- At the same time it will generate a proposed blocking strategy
```logos
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
This output shows that there are currently no IDS blocks (Temp Blocked IPs), There are currently 888 Perma Blocks, of which 884 are IP addresses and 4 are CIDR groups, on the server's blocklist.

5. Type `Make` to generate the script that would implement the proposed blocking actions
	- You should see something like `Script saved to file: [path of executable]/commit_bans.txt`

Example Script:
```logos
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

6. To execute the script, type `run commit_bans.txt`

### Persisting settings and configuring a startup script
At the prompt, enter `settings` and follow the prompts to modify the settings or edit the config.json file

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

- **VirusTotalApiKey:** (Optional) API key to your free or better Virus Total Account
	- See: [Virus Total](https://www.virustotal.com/)
	- Virus Total allows more accurate identification of CIDR ranges
	- Without an API key, IPs are categorized into /24 and /16 networks only
- **LoggingLevel:** Valid values (Debug, Info, Warning, Error)
- **Protocol:** http or https
	- The HTTP protocol to use when communicating with the Smarter Mail API
- **UseAutoTokenRefresh:** true or false
	- If true, the client will maintain an active connection
	- If false, the API wrapper (SmartMailApiClient) will refresh the token as needed upon the next request
- **PercentAbuseTrigger:** A number between 0 and 1 that triggers CIDR blocking if the subnet exceeds this percentage of bad actor IPs.
	- The settings command limits this to .005 to 1, as .005 in a /24 subnet requires at least 2 IPs to trigger blocking.
- **UseAutoLogin:** If true, attempt to connect to the server at launch using the AutoLoginUsername and AutoLoginPassword.
- **AutoLoginUsername:** The account to login with
- **AutoLoginPassword:** The password to the account
- **StartupScript:** An array of strings, where each element represents a single line in a startup script
	- Can be used to configure the CLI to run a set of commands and schedule startup jobs.

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
- Schedules the `docmany` command to run every 70 seconds and document up to `4` IPs on the block list using Virus Total data
- Schedules the `make` command to run every 10 minutes, generating the `commit_bans.txt` script
- `Wait` 5 seconds so that the `run` command will always run 5 seconds after the `make` command
- Schedules the `run` command to run every 10 minutes and execute the script `commit_bans.txt`
- Loads the server data into memory
- Displays scheduled jobs

### About Virus Total
Currently, the CLI is hard-coded to the free account quota limits. However, if you're paying for a Virus Total account and have higher quota limits, you can modify the Globals.cs file to set the limits as you would like. I'll probably refactor this and make it configurable soon, but that's how it works right now.

Quota limits are set in the Globals.cs file:
```csharp
public static int vtMinuteQuota { get; set; } = 4;
public static int vtDailyQuota { get; set; } = 500;
```

If you launch with the above script, there may be a period where it's collecting data on all the IPs in your block list from Virus Total. The free account gives you 4 hits/minute and 500 hits/day, so you may go over this initially. That's okay because a built-in quota tracking system prevents further hits against the API when you're over your quota. This system ensures that your usage is always within the set limits. When the quota period expires, it will continue documenting IPs without any intervention required. Currently its hard coded to use the free account values, but if you modify the Globals.cs file, you can set your own quota limits.

Unfortunately, Virus Total does not give free accounts access to the endpoint that gives quota information, and they don't return quota information in the headers. There is also a 15.5k hits/month quota, but I did not implement that. Hopefully, I won't need to. If an API request detects that the quota was reached, it will not allow further hits until the next day. The day quota expires at midnight UTC, and the month quota will be reset at midnight UTC on the 1st of the month.

### Is it working?
Let it run for a few days if needed or forever. You should notice that your IDS list remains primarily empty, only containing IPs that were last detected within 10 minutes, while your block list will continue to grow.  

IP descriptions on the block list are initially set to the basic information available from the IDS block, e.g., what protocol, country, etc., the IP address was found using. Every time `docmany 4` executes, it will look for up to 4 undocumented IPs in the block list, query Virus Total, and refresh their descriptions. History is saved to `IpInfo.json`, and future lookups against an IP will come from that file instead of using up a hit on the Virus Total API.  To completely forget an IP, remove it from this file and the server's block list. 

Once the IP has been documented, it will have an accurate CIDR block assigned to it and can now be considered by the `make` command for inclusion in the commit_bans.txt script file.

If you examine the POP, IMAP, or SMTP logs, you can see that it is working.  SMTP is likely the most active.

Sample SMTP log for a single blocked request:
```logos
15:24:36.585 [80.244.11.121][60044376] connected at 6/6/2024 3:24:36 PM
15:24:36.585 [80.244.11.121][60044376] "421 Server is busy, try again later." response returned.
15:24:36.585 [80.244.11.121][60044376] IP is blacklisted
15:24:36.585 [80.244.11.121][60044376] disconnected at 6/6/2024 3:24:36 PM
```
If you see this going on, then it's working.

## Commands
Unless otherwise noted, all commands can be used in a script, in interactive mode, or from the command line.

### Clear

| Command Aliases | Comments|
| :--- | :--- |
| `clear` `cls` | Clears the screen, can be used from the command line, but is mostly used for interactive mode.|

### CommitProposed

| Command Alias | Comments |
| :--- | :--- |
| `CommitProposed` `cp` | Commits proposed changes from the current cache (loaded with load command). Cleans up the IDS blocks and condenses all perma blocks into CIDR ranges where possible. |

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
- **noload:** Prevents the cache from reloading after execution. This is useful in scripts where many doc commands are issued.  Issue a `load` command to reload the cache later.
- **nosave:** Prevents saving of Virus Total data to the IpInfo.json file after execution. This is useful in scripts where many doc commands are issued.
	- Issue a `SaveIpInfo` command to reload the cache later.

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

### Ignore

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| Ignore | IP/CIDR or List | Toggles the Ignore status of an IP or CIDR address |

_Maintains a list in `ipIgnore.json`, these IPs will be removed from the server's blacklist if discovered_

Example: 
- `ignore 127.0.0.1 Some Description` will add this IP, with a description, to the Ignore list. Issuing this command a second time will remove it from the ignore list.
- `ignore 127.0.0.0/24 Some Description` will ignore all IPs in the 127.0.0.x network.
- `ignore list` will display the current ignore list

_This command can be useful if you have a device or subnet that you know you never want to block but that can occasionally fall into an IDS block. You could also use the server's white-list to do this.  IPs/CIDRs in this list will be automatically removed when generating the blocking script with `make`._

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

_Used in scripts to make sure the cache appears invalid for follow-on commands_

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

_This data is analyzed to find subnets when identifying groups of malicious IPs_

### Login

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Login` | Username Password | Logs into the API, retrieving auth tokens for the current session |

_Refresh token tracking starts upon successful login_

### MakeScript

| Command Alias | Comments |
| :--- | :--- |
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

### Restore

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `Restore` | IpFragment or * | Restores the black list from previously stored data. |

_Searches the IP history data (IpInfo.json) for IPs that start with IpFragment or all IPs if * is used, and restores the individual IP blocks if they are missing._

For Example: 
- _`Restore *` will restore all IPs and remove all CIDR subnets_
- _`Restore 192.168.1` will restore all IPs that start with 192.168.1 and remove any CIDR groups that match on 192.168.1_
- _`Restore 192.168.1.0/24` will restore all known IPs in the 192.168.1.x subnet and remove any CIDR groups that either match or are contained by 192.168.1.0/24_

_**Note:** After a restore, `make` may want to re-create CIDR blocks and remove IPs from the block list, depending on how your settings are configured_

### RunScript

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `RunScript` `run` `exe` | FullPathToScript | Executes a saved script EG: RunScript C:\temp\myscript.txt or run c:\temp\myscript.txt |

### SaveIpInfo

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `SaveIpInfo` | Saves Cached IP info from Virus Total to file `ipinfo.json` |

### SetOption

| Command Alias | Params | Comments |
| :--- | :--- | :--- |
| `SetOption` | option value | Sets a system option to the specified value |

| Option | Values | Description |
| :--- | :--- | :--- |
| `loglevel` | `debug` `info` `warning` `error` | Sets the current logging level to the specified value |
| `progress` | `on` `off` | if on, will display a progress indicator while scripts are running |

Example:
- `setoption loglevel debug` will switch to showing command output of debug level or higher
- `setoption progress on` will show the progress indicator when a script is running

These two options can be used together to suppress output while a script is running and instead display `Action....` style messages as the script progresses.
Example script:
```
setoption loglevel warning
setoption progress on
# do stuff here
setoption loglevel Info
setoption progress off
```

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

_**Note:** This command requires the program to run in interactive mode.  If run from the command line, the app will enter interactive mode._

### Session

| Command Alias | Comments |
| :--- | :--- |
| `Session` | Displays info about the current session |

_This displays information about the currently logged-on user_

### Settings

| Command Alias | Comments |
| :--- | :--- |
| `Settings` `set` | Configures settings |

_Settings are saved to `config.json`_

### UpdateBlacklist

| Command Alias | Comments |
| :--- | :--- |
| `UpdateBlacklist` | Builds a blocking script and executes it in one step |

_This is equivalent to running: `Make` followed by `run commit_bans.txt`_
_If you would rather examine the script before execution, then run `make` by itself first._

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

# Extending the API

Extending the API should be straightforward.  The approach is to:
- Pick an endpoint you are interested in
- Create any models to send/receive for that endpoint
- Add any required methods to the API to use the endpoint

The IResponse interface is used to give the caller generalized information about the status of a request.

```csharp
public interface IResponse
{
    public bool success { get; set; }
    public Error? ResponseError { get; set; }
}
```
_Currently used in the CLI by the `CommandBase` class, to give all commands the ability to check the results of an API call_

For example, the login response:
```csharp
public class LoginResponse : IResponse
{
    public string emailAddress { get; set; } = string.Empty;
    public bool changePasswordNeeded { get; set; } = false;
    public bool displayWelcomeWizard { get; set; } = false;
    public bool isAdmin { get; set; } = false;
    public bool isDomainAdmin { get; set; } = false;
    public bool isLicensed { get; set; } = false;
    public string autoLoginToken { get; set; } = string.Empty;
    public string autoLoginUrl { get; set; } = string.Empty;
    public string localeId { get; set; } = string.Empty;
    public bool isImpersonating { get; set; } = false;
    public bool canViewPasswords { get; set; } = false;
    public string accessToken { get; set; } = string.Empty;
    public string refreshToken { get; set; } = string.Empty;
    public DateTime? accessTokenExpiration { get; set; }
    public string username { get; set; } = string.Empty;
    public bool success { get; set; } = false;
    public int resultCode { get; set; } = -1;

    public Error? ResponseError { get; set; }
}
```
The properties of this class are intentionally spelled the same as the response values returned from the server. This allows easy deserialization from the JSON response to the LoginResponse object.

Under `Models.Requests`, you can find the Credential object, which is used to send a login request to the server.
```csharp
public class Credential
{
    public string username { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}
```
Again, the spelling of the properties is specific to the server's expected JSON spelling.

Then, we can put it all together by creating the supporting method for the `ApiClient` class.
```csharp
public async Task<LoginResponse> Login(string username, string password)
{
    string apiPath = "api/v1/auth/authenticate-user";
    var loginRequest = new Models.Requests.Credential() { username = username, password = password };

    var result = await ExecuteRequest(httpMethod: HttpMethod.Post, requestUri: apiPath, requestObj: loginRequest);

    var response = JsonConvert.DeserializeObject<LoginResponse>(result.data);
    if (response == null || !response.success)
    {
        response = new LoginResponse()
        {
            resultCode = response?.resultCode ?? 401,
            ResponseError = result.error
        };
    }

    return response;
}
```
Here, we can see the basic pattern for an API method:
- Set the path to the API endpoint we want to use
	- `string apiPath = "api/v1/auth/authenticate-user";`
- Create the request object
	- `var loginRequest = new Models.Requests.Credential() { username = username, password = password };`
- Pass these values to the ExecuteRequest method with the proper verb
	- `var result = await ExecuteRequest(httpMethod: HttpMethod.Post, requestUri: apiPath, requestObj: loginRequest);`
- Convert the response into the IReponse object
	- `var response = JsonConvert.DeserializeObject<LoginResponse>(result.data);`
- If the response was not successful, then populate the error data
```csharp
response = new LoginResponse()
{
    resultCode = response?.resultCode ?? 401,
    ResponseError = result.error
};
```

This same basic pattern can be used to implement as many of the API endpoints as you would like.

# Extending the CLI
While adding new commands to the CLI may not be quite as straightforward as adding endpoints to the API, the process is fairly simple. The more complex part is figuring out how those commands should interact with the internals of the CLI utility.

Every command inherits from `CommandBase`, `ICommand`, and `ICommandFactory`

Let's take a look at this very basic example of implementing the `Print` command:
```csharp
public class PrintCommand : CommandBase, ICommand, ICommandFactory
{
    private readonly string message;

    public string CommandName => "Print";
    public string CommandArgs => "message";
    public string[] CommandAlternates => [];
    public string Description => "Prints a message to the output";
    public string ExtendedDescription => "";

    public PrintCommand()
        : base(Globals.Logger)
    {
        message = "";
    }
    public PrintCommand(string message)
        : base(Globals.Logger)
    {
        this.message = message;
    }

    public ICommand MakeCommand(string[] args)
    {
        if (args.Length <= 1)
            return new PrintCommand();
        else
            return new PrintCommand(string.Join(" ", args.Skip(1)));
    }

    public void Run()
    {
        Log.Prompt($"\n{message}");
    }
}
```

## ICommandFactory properties
When building a new command, we must set the following properties:
- Inherit from the base class and interfaces
    - `public class PrintCommand : CommandBase, ICommand, ICommandFactory`
- Provide default values for the CommandName, CommandArgs, CommandAlternates, and any Description/Extended Description
    - `public string CommandName => "Print";` sets the full name of the command
    - `public string CommandArgs => "message";` lists the arguments that can be used with the command
    - `public string[] CommandAlternates => [];` an array of strings that represents alternates for this command
        - EG: `["pt","?"]` could be used to alias the print command to `pt` and `?`.
    - `public string Description => "Prints a message to the output";` is the basic description of this command
    - `public string ExtendedDescription => "";` is the extended description of this command

The `Help` command uses these values to display documentation about this command. `CommandName` and `CommandAlternates` are used to parse the commandline and locate this command.

## Constructor(s)
Each constructor should call `: base(Globals.Logger)` to inject the logger and configure any basic settings.

When a command is located by the parser, the `MakeCommand` method is called, passing in any arguments required to configure that command.  Depending on the arguments, you may need one or more constructors. In this case, we need two.  One for when no arguments are passed and one for when arguments are passed.

```csharp
public ICommand MakeCommand(string[] args)
{
    if (args.Length <= 1)
        return new PrintCommand();
    else
        return new PrintCommand(string.Join(" ", args.Skip(1)));
}
```

- Since args should always have at least one element, the command name itself, then we can return the basic command if 1 or fewer arguments are passed: 
```csharp
if (args.Length <= 1)
    return new PrintCommand();
```
- In every other case, there are 2 or more args, so we treat the rest as the message to use for this command and use the second constructor to configure it
```csharp
else
    return new PrintCommand(string.Join(" ", args.Skip(1)));
```
- Here, the second constructor internalizes the message that it should display:
```csharp
public PrintCommand(string message)
    : base(Globals.Logger)
{
    this.message = message;
}
```

Finally, if the command parses and everything is ok, then the `Run` method is called from the `ICommand` interface and is where the actual work this command does is implemented:
```csharp
public void Run()
{
    Log.Prompt($"\n{message}");
}
```

With all this done, we can now:
- Type `Print Hello World!` into the command prompt, and it will do just that.
- Type `Help Print` into the command prompt, and it will display the information about how to call the command and its description(s)

## Command naming
Commands are searched for by:
- An exact match of any of the `CommandAlternates`
- If that fails, then a starts with match is done on the `CommandName`

If, for some reason, two commands are returned, either because you used a duplicate CommandAlternate or because two or more CommandName(s) start the same, then it will fail to execute and display information about what possible commands you might have meant.  

For example, if you enter `p hello world`, you might expect it to print hello world, but instead, you will get the following back:
```logos
Did you mean one of these?
Print                    -Prints a message to the output
PermaBan                 -Adds or Updates an IP to the permanent black list
```
Because both commands start with `P` and neither command is an exact match for `P`.

## CommandBase
The `CommandBase` object is provided to give every command some basic configuration and API status methods.  
See: `SmartMail.Cli.Commands.CommandBase`

```csharp
/// <summary>
/// Base class for commands
/// </summary>
public abstract class CommandBase
{
    private readonly ICommandLogger _logger;

    /// <summary>
    /// Any command that writes to shared data used by other commands should set this to false in their constructors
    /// </summary>
    public bool IsThreadSafe { get; internal set; }
    public ICommandLogger Log { get { return _logger; } }
    public bool RequiresInteractiveMode { get; set; } = false; //Assume commands can be run from the commandline
    protected CommandBase(ICommandLogger logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Commands that process api responses can use this to test if the response was ok
    /// </summary>
    /// <param name="response">The response returned from an API</param>
    /// <returns>True if the response is ok</returns>
    public bool IsResponseOk(IResponse? response)
    {
        if (response != null && response.success)
        {
            return true;
        }
        else if (response != null && response.ResponseError != null)
        {
            Log.Error($"Failed: {response.ResponseError}");
        }
        else
        {
            Log.Error("Unknown Error");
        }

        return false;

    }

    /// <summary>
    /// Commands that access the api can use this to test if the connection to the server is ok
    /// </summary>
    /// <param name="apiClient">A SmarterMail api client object</param>
    /// <returns>True if currently connected to the api, False if you need to login first</returns>
    public bool IsConnectionOk(ApiClient? apiClient)
    {
        if (apiClient != null &&
            apiClient.Session != null &&
            !string.IsNullOrEmpty(apiClient.Session.AccessToken))
        {
            return true;
        }
        else
        {
            Log.Error("You must be logged in to use this command");
            return false;
        }
    }
}
```

This is fairly well documented internally, however, here is some discussion on what it does:

### Properties
- `IsThreadSafe`: Defaults to True but should be set to false in any constructors where that command will update data used by another command.  
  - It allows thread blocking to occur if this command is active when another command fires, making the new command wait for the current command to finish.
    - The command `load`, for example, is not thread-safe since it loads data into memory and generates a blocking strategy.
    - If other commands that rely on the data in memory or that would update the data in memory were to fire at the same time, then this will make those commands wait until the `load` command finishes.
- `Log`: Points at the ICommandLogger interface that is used by all commands to log output
- `RequiresInteractiveMode`: This should be set to true if this command requires interactive mode. If launched from the command line, these commands will cause a shell to be launched to support them.
    - The command `sched`, for example, requires interactive mode since scheduled jobs require an active, always-running instance of the CLI in order to execute scheduled jobs.

### Methods
- `public bool IsResponseOk(IResponse? response)`: Used by commands to generalize response success checking.
- `public bool IsConnectionOk(ApiClient? apiClient)`: Used by commands to generalize checking for an API connection

These methods eliminate the need to do this logic in every command that uses the API.

For example, consider the `Run` method on the `GetTempIpBlockCounts` command:
```csharp
public void Run()
{
    Log.Debug("---- Blocked IP Counts ----");
    if (IsConnectionOk(Globals.ApiClient))
    {
        var r = Globals.ApiClient?.GetCurrentlyBlockedIPsCount().ConfigureAwait(false).GetAwaiter().GetResult();

        if (IsResponseOk(r))
        {
            Cache.TempBlockCounts = r.counts;
            foreach (var c in Cache.TempBlockCounts)
                Log.Info($"{c.Key.PadLeft(20)}:{c.Value.ToString().PadLeft(5)}");

            Log.Info($"Total IDS(Temporary) blocks:".PadLeft(21) + $"{Cache.TotalTempBlocks}".PadLeft(5) + " Found");
        }
    }
    Log.Debug("---- Blocked IP Counts Done ----");
}
```
- If a command requires a connection to the API before it can be executed, call `IsConnectionOk` passing in the API client.
```csharp
if (IsConnectionOk(Globals.ApiClient))
{
    ...
}
```
- If that succeeds, call the API and check the response with `IsResponseOk`.
```csharp
if (IsResponseOk(r))
{
    ...
}
```
- If both conditions are good, then you can process the response. In this case, it stores the retrieved data in memory and displays it at the LogLevel of Info.
```csharp
Cache.TempBlockCounts = r.counts;
foreach (var c in Cache.TempBlockCounts)
    Log.Info($"{c.Key.PadLeft(20)}:{c.Value.ToString().PadLeft(5)}");

Log.Info($"Total IDS(Temporary) blocks:".PadLeft(21) + $"{Cache.TotalTempBlocks}".PadLeft(5) + " Found");
```

# Credits
Credit to: @github/jsakamoto for creating a robust IP address range calculator (https://github.com/jsakamoto/ipaddressrange/).
