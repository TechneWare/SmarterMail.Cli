using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Displays session info for the currently connected SmarterMail user
    /// </summary>
    public class SessionInfoCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "Session";

        public string CommandArgs => "";

        public string[] CommandAlternates => [];

        public string Description => "Displays info about the current session";

        public string ExtendedDescription => "";

        public SessionInfoCommand()
        : base(Globals.Logger) { }

        public ICommand MakeCommand(string[] args)
        {
            return new SessionInfoCommand();
        }

        public void Run()
        {
            Log.Info("---- Session Info -----");
            var sessionProps = new Dictionary<string, string>();
            if (IsConnectionOk(Globals.ApiClient))
            {
                var type = Globals.ApiClient!.Session!.GetType();
                foreach (var prop in type.GetProperties())
                {
                    var val = prop.GetValue(Globals.ApiClient.Session, new object[] { });
                    var valStr = val == null ? "" : val!.ToString();
                    sessionProps.Add(prop.Name, valStr);
                }

                foreach (var prop in sessionProps)
                {
                    Log.Info($"{prop.Key.PadLeft(15)}:{prop.Value.PadLeft(20)}");
                }
            }
            else
            {
                Log.Info($"Not Logged in - no session data available");
            }

        }
    }
}
