# SmarterMail.Cli

A Simple Commandline utility that I created to manage my IP Black List on a SmarterMail server using their API

Which grew into something that could be extended to perform any action you wanted against a Smarter Mail server
With features such as a scripting engine and scheduled jobs
It can be run interactivly, where the user can issue commands
Or it can be run from command line, so the utility can be used in other scripts

See: https://www.techtarget.com/searchnetworking/definition/CIDR on what CIDR is.
The original use case, was to examine all the blocked IPs and see if any of them can be grouped together into a subnet or CIDR group
By entering IPs such as 1.2.3.0/24 into your black list, you can effectivly block all the IPs on the 1.2.3.x subnet with a single entry

 
See: https://www.virustotal.com/
Optionaly, it can gather IP information from Virus Total to catalog every IP ever found and target CIDR ranges more accuratly when blocking. 
And allowing rebuilds of the server's black list from scratch using this data. 

This has two main benefits:
1. Since the subnet being blocked has a bad reputation, any furthar abuse from that subnet will not generate new IDS or temporary blocks
2. Since the list is now shorter, it takes the server less time to search it when blocking future requests
And possibly
3. Every IP listed in the black list now has consistent documentation in the description
4. automatic black listing is possible. Just check in on your black list from time to time. No more carple clicking to edit the list.

Or if you don't trust it, its also possible to have it just generate the script to do the blocking and let you read it first.

The trick of course is to accuratly identify subnets, and thereby the subnet size and thus be able to calculate a percentage of abuse in a given subnet.
Currently there are two approaches used.
If relying only on Smarter Mail data, then IP subnet matching will only be against a /24 or /16 subnet
If using Virus Total data, then you get a more refined CIDR and thus can more accuratly calculate when to implement a CIDR block
Percent Abuse is the number of abusing IPs divided by the useable block size of a given subnet EG: %Abuse = badIPs / subnetSize
If you set a trigger threshold of .01, then it would want to block a /24 subnet when 3 or more bad IPs show up in that subnet or it would take 650 for a /16 subnet.
Thus the more accuratly you can get on the /xx of a subnet (CIDR), the better the scaling of the blocking trigger, 
and why I recommend using Virus Total for the added accuracy.
