using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Removes a Black Listed IP (perma ban)
    /// </summary>
    public class DeletePermaBlockedIpCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string ipAddress;

        public string CommandName => "DeleteBan";

        public string CommandArgs => "IpAddress/CIDR";

        public string[] CommandAlternates => ["db"];

        public string Description => "Removes an IP/CIDR from the permanent blacklist";

        public string ExtendedDescription => "";

        public DeletePermaBlockedIpCommand()
            : base(Globals.Logger)
        {
            ipAddress = "";
        }
        public DeletePermaBlockedIpCommand(string IpAddress)
            :base(Globals.Logger) 
        {
            ipAddress = IpAddress;
        }
        public ICommand MakeCommand(string[] args)
        {
            if(args.Length <= 1)
                return new DeletePermaBlockedIpCommand();
            else if (args.Length != 2)
                return new BadCommand("Invalid Syntax - Use EG:DeleteBan 127.0.0.1 or db 127.0.0.0/24");

            return new DeletePermaBlockedIpCommand(args[1]);
        }

        public void Run()
        {
            if (IsConnectionOk(Globals.ApiClient))
            {
                Log.Info($"Deleting Perma Block on [{this.ipAddress}]");
                var r = Globals.ApiClient?.DeletePermaBlockedIP(this.ipAddress).ConfigureAwait(false).GetAwaiter().GetResult();

                if (IsResponseOk(r))
                    Log.Debug($"Done Deleting Perma Block on [{this.ipAddress}]");
            }
        }
    }
}
