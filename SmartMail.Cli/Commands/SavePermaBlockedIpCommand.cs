using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Saves an IP with a description to the SmarterMail black list (perma ban)
    /// </summary>
    public class SavePermaBlockedIpCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string ip;
        private readonly string description;

        public string CommandName => "PermaBan";

        public string CommandArgs => "IP/CIDR Description";

        public string[] CommandAlternates => ["pb"];

        public string Description => "Adds or Updates an IP to the permanent black list";

        public string ExtendedDescription => "EG: PermaBan 192.168.1.1 I banned this IP\nPermaBan 192.168.1.0/24 I banned this CIDR";

        public SavePermaBlockedIpCommand()
            : base(Globals.Logger)
        {
            this.ip = "0.0.0.0";
            this.description = "Empty Description";
        }
        public SavePermaBlockedIpCommand(string ip, string description)
            : base(Globals.Logger)
        {
            this.ip = ip;
            this.description = description;
        }
        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new SavePermaBlockedIpCommand();
            else if (args.Length < 3)
                return new BadCommand("You must provide an IP/CIDR and a description");

            var description = string.Join(" ", args.Except([args[0], args[1]]));

            return new SavePermaBlockedIpCommand(args[1], description);
        }

        public void Run()
        {
            if (IsConnectionOk(Globals.ApiClient))
            {
                Log.Info($"Perma Blocking[{this.ip}]");
                var r = Globals.ApiClient?.SavePermaBlockedIP(this.ip, this.description).ConfigureAwait(false).GetAwaiter().GetResult();

                if (IsResponseOk(r))
                    Log.Debug($"Done Perma Blocking[{this.ip}]");
            }
        }
    }
}
