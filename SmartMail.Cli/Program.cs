using SmartMail.Cli;
using SmartMail.Cli.Commands;
using SmartMail.Cli.Models;

internal class Program
{
    /// <summary>
    /// Main Entry Point for the SmartMail api client
    /// </summary>
    /// <remarks>
    /// A Simple Commandline utility that I created to manage my IP Black List on a SmarterMail server using their API
    /// 
    /// Which grew into something that could be extended to perform any action you wanted against a Smarter Mail server
    /// With features such as a scripting engine and scheduled jobs
    /// It can be run interactivly, where the user can issue commands
    /// Or it can be run from command line, so the utility can be used in other scripts
    /// 
    /// See: https://www.techtarget.com/searchnetworking/definition/CIDR on what CIDR is.
    /// The original use case, was to examine all the blocked IPs and see if any of them can be grouped together into a subnet or CIDR group
    /// By entering IPs such as 1.2.3.0/24 into your black list, you can effectivly block all the IPs on the 1.2.3.x subnet with a single entry
    /// 
    ///  
    /// See: https://www.virustotal.com/
    /// Optionaly, it can gather IP information from Virus Total to catalog every IP ever found and target CIDR ranges more accuratly when blocking. 
    /// And allowing rebuilds of the server's black list from scratch using this data. 
    /// 
    /// This has two main benefits:
    /// 1. Since the subnet being blocked has a bad reputation, any furthar abuse from that subnet will not generate new IDS or temporary blocks
    /// 2. Since the list is now shorter, it takes the server less time to search it when blocking future requests
    /// And possibly
    /// 3. Every IP listed in the black list now has consistent documentation in the description
    /// 4. automatic black listing is possible. Just check in on your black list from time to time. No more carple clicking to edit the list.
    /// 
    /// Or if you don't trust it, its also possible to have it just generate the script to do the blocking and let you read it first.
    /// 
    /// The trick of course is to accuratly identify subnets, and thereby the subnet size and thus be able to calculate a percentage of abuse in a given subnet.
    /// Currently there are two approaches used.
    /// If relying only on Smarter Mail data, then IP subnet matching will only be against a /24 or /16 subnet
    /// If using Virus Total data, then you get a more refined CIDR and thus can more accuratly calculate when to implement a CIDR block
    /// Percent Abuse is the number of abusing IPs divided by the useable block size of a given subnet EG: %Abuse = badIPs / subnetSize
    /// If you set a trigger threshold of .01, then it would want to block a /24 subnet when 3 or more bad IPs show up in that subnet or it would take 650 for a /16 subnet.
    /// Thus the more accuratly you can get on the /xx of a subnet (CIDR), the better the scaling of the blocking trigger, 
    /// and why I recommend using Virus Total for the added accuracy.
    /// 
    /// </remarks>
    /// <param name="args"></param>
    private static void Main(string[] args)
    {

        if (Globals.Settings.UseAutoLogin)
        {
            // Login credentials can be found in the config.json and set with the settings command
            // Alternatively use the Login [Username] [Password] command from the prompt
            var loginCmd = new LoginCommand(Globals.Settings.AutoLoginUsername, Globals.Settings.AutoLoginPassword);
            loginCmd.Run();
        }

        try
        {
            //Initilize from ipinfo.json - contains all api responses from Virus Total's api
            //Used to document each IP address and potentially rebuild the primary black list
            Cache.LoadIpInfoes();
        }
        catch (Exception ex)
        {
            Globals.Logger.Error(ex.Message);
        }

        Script? startupScript = null;
        if (Globals.Settings.StartupScript.Length > 0)
        {
            //Scripts are just lists of commands, there is no conditional branching or looping logic at this time
            //The startup script can be found in the config.json file
            startupScript = new Script(Globals.Logger, Globals.Settings.StartupScript, "Startup");
            startupScript.Parse();
        }

        ICommand? startCommand = null;
        // If no arguments, assume interactive mode, which presents the user with a prompt to enter commands
        if (args.Length == 0)
        {
            startupScript?.Run();

            //run in interactive mode
            startCommand = new InteractiveCommand()
                .MakeCommand(args);
        }
        else
        {
            //run in commandline mode
            var availCommands = Utils.GetAvailableCommands();
            var parser = new CommandParser(availCommands);
            startCommand = parser.ParseCommand(args);

            var requiresInteractiveMode = ((CommandBase)startCommand).RequiresInteractiveMode;
            if (requiresInteractiveMode)
            {
                var commandName = ((ICommandFactory)startCommand).CommandName;
                Globals.Logger.Warning($"===== Interactive Mode Required: {requiresInteractiveMode} for {commandName} =====");

                args = ["interactive", .. args];
                startCommand = parser.ParseCommand(args);

                Globals.Logger.Debug($"Args:{String.Join(" ", args)}");
            }

            //If settings are invoked from the commandline, then skip the startup script
            if (startCommand.GetType() != typeof(SettingsCommand))
                startupScript?.Run();
        }

        //This should not happen, but if it does, print usage
        if (startCommand == null)
        {
            var availCommands = Utils.GetAvailableCommands();
            Utils.PrintUsage(availCommands);
        }
        else 
        {
            //Otherwise run the startCommand
            startCommand.Run();
        }
    }
}