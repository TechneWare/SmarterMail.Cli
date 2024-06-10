using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    public class IgnoreCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string ip;
        private readonly string description;
        private readonly bool showList;

        public string CommandName => "Ignore";

        public string CommandArgs => "Ip/CIDR Description";

        public string[] CommandAlternates => [];

        public string Description => "Toggles the Ignore status of an IP or CIDR address";

        public string ExtendedDescription => "Maintains a list in ipIgnore.json, these IPs will be removed from the server's blacklist if discovered";

        public IgnoreCommand()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
        }
        public IgnoreCommand(string ip, string description)
            : base(Globals.Logger)
        {
            this.ip = ip;
            this.description = description;
        }
        public IgnoreCommand(bool showList)
            : base(Globals.Logger)
        {
            this.showList = showList;
        }
        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new IgnoreCommand();
            else
            {
                string ip = args[1];
                string description = string.Join(" ", args.Except([args[0], args[1]])).Trim();

                if (ip.ToLower() == "list")
                    return new IgnoreCommand(showList: true);
                else
                    return new IgnoreCommand(ip, description);
            }
        }

        public void Run()
        {
            if (showList)
            {
                foreach (var ip in Cache.IgnoredIps)
                    Log.Prompt($"\n{ip.LastUpdated,-25}{ip.Ip,-20}{ip.Description}");
                Log.Prompt("\n");
            }
            else
                Cache.IgnoreIp(ip, description);
        }
    }
}
