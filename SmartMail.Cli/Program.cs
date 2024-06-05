using SmartMail.Cli;
using SmartMail.Cli.Commands;
using SmartMail.Cli.Models;

internal class Program
{
    /// <summary>
    /// Main Entry Point for the SmartMail api client
    /// </summary>
    /// <param name="args">Optional commandline args</param>
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