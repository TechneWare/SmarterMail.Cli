# SmarterMail.Cli

## Overview
This is a solution with two projects
- A REST client for the Smarter Mail Server API
- A simple command line utility to manage an IP Black List on a SmarterMail server using the server's API

Both projects have the potential to be extended and provide more features and automation against a Smarter Mail server than simply managing the blocklist. However, I created this initial version to solve my use case: automated blocklist management.

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

| CIDR | Size | #IPs to block |
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
      		- I tried using 64bit Arm on a Raspberry PI 4b, but I believe there is a bug with 64bit compiling at this time, so I ended up using the 32bit version. It could also be that my Raspberry PI has had its OS upgraded in place serveral times, and so, even though the OS reports as being 64bit, it only wants to work with 32bit compiled ARM.  Try both ways if your currious.
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
DeleteBan [IpAddress/CIDR]        - Removes an IP/CIDR from the permanent blocklist
DeleteTemp [IpAddress]            - Removes an IP from the temporary blocklist
DMany [number]                    - Documents 1 or more undocumented perma blocked IPs
Doc [IpAddress protocol noload nosave]- Adds known IP Info to an IPs description in the servers settings>security>black list
IpInfo [IpAddress]                - Attempts to retrieve IP Info from Virus Total for the specified IP Address
GetPermaBlockIps [show]           - loads permanently blocked IP addresses to memory
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
Restore [IpFragment | *]          - Restores the black list from previously stored data.
RunScript [FullPathToScript]      - Executes a saved script EG: RunScript C:\temp\myscript.txt or run c:\temp\myscript.txt
SaveIpInfo                        - Saves Cached IP info from VirusTotal to file ipinfo.json
PermaBan [IP/CIDR Description]    - Adds or Updates an IP to the permanent blacklist
Sched [#Seconds commandToRun]     - Schedules a job to run on an interval until stopped
Session                           - Displays info about the current session
Settings                          - Configures settings
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

_**Note:** After a restore, `make` may want to re-create CIDR blocks and remove IPs from the block list, depending on how your settings are configured_

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

_**Note:** This command requires the program to run in interactive mode.  If run from the command line, the app will enter interactive mode._

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
