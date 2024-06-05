using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Allows setting settings - interactive input to build the config.json file
    /// </summary>
    public class SettingsCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "Settings";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["set"];

        public string Description => "Configures settings";

        public string ExtendedDescription => "settings are saved to config.json";

        public SettingsCommand()
            : base(Globals.Logger)
        {

        }

        public ICommand MakeCommand(string[] args)
        {
            return new SettingsCommand();
        }

        public void Run()
        {
            var settings = Settings.GetSettings();

            settings.LoggingLevel = Enum.TryParse(Utils.InputPrompt(settings.LoggingLevel.ToString(), "Log Level [Debug|Info|Warning|Error]"), out ICommandLogger.LogLevelType l) ? l : ICommandLogger.LogLevelType.Debug;
            settings.Protocol = Enum.TryParse(Utils.InputPrompt(settings.Protocol.ToString(), "Protocol [http|https]"), out SmartMailApiClient.HttpProtocol p) ? p : SmartMailApiClient.HttpProtocol.https;
            settings.ServerAddress = Utils.InputPrompt(settings.ServerAddress, "Server domain/IP address");
            settings.UseAutoTokenRefresh = bool.TryParse(Utils.InputPrompt(settings.UseAutoTokenRefresh.ToString(), "Auto Refresh Auth Tokens? "), out _);

            settings.PercentAbuseTrigger = double.TryParse(Utils.InputRange(.001, 1, () => Utils.InputPrompt(settings.PercentAbuseTrigger.ToString(), "Percent Abuse Trigger")), out double v) ? v : 0;

            settings.VirusTotalApiKey = Utils.InputPrompt(settings.VirusTotalApiKey, "VirusTotal API Key");

            settings.UseAutoLogin = bool.TryParse(Utils.InputPrompt(settings.UseAutoLogin.ToString(), "Login on Launch?"), out _);

            if (settings.UseAutoLogin)
            {
                settings.AutoLoginUsername = Utils.InputPrompt(settings.AutoLoginUsername, "User Name");
                settings.AutoLoginPassword = Utils.InputPrompt(settings.AutoLoginPassword, "Password");
            }

            Log.Info("\nStartup Script: (Enter one command per line, space to remove a line, empty to end adding lines)");

            string newLine = "";
            var script = new Script(Log, "Startup");

            // keep/change existing lines
            var lineIsValid = false;
            for (int i = 0; i < settings.StartupScript.Length; i++)
            {
                lineIsValid = false;
                do
                {
                    newLine = Utils.InputPrompt($"{settings.StartupScript[i]}", "$>");
                    lineIsValid = newLine.StartsWith(' ') || string.IsNullOrEmpty(newLine) || script.Add(newLine);
                } while (!lineIsValid);

                settings.StartupScript[i] = newLine;
            }

            //add new lines
            newLine = "";
            do
            {
                lineIsValid = false;
                newLine = Utils.InputPrompt($"", "$>");
                lineIsValid = string.IsNullOrEmpty(newLine) || script.Add(newLine);
                settings.StartupScript = [.. settings.StartupScript, newLine];
            } while (!lineIsValid || !string.IsNullOrEmpty(newLine));

            //cleanup before save
            settings.StartupScript = settings.StartupScript.Where(s => !string.IsNullOrEmpty(s.Trim())).ToArray();

            //Make sure to use the new settings
            Globals.Settings = Settings.SaveSettings(settings);
        }
    }
}
