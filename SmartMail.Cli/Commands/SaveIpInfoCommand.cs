using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    public class SaveIpInfoCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "SaveIpInfo";

        public string CommandArgs => "";

        public string[] CommandAlternates => [];

        public string Description => "Saves Cached IP info from VirusTotal to file ipinfo.json";

        public string ExtendedDescription => "";
        public SaveIpInfoCommand()
            :base(Globals.Logger)
        {
            
        }

        public ICommand MakeCommand(string[] args)
        {
            return new SaveIpInfoCommand();
        }

        public void Run()
        {
            Log.Info("Saving VirusTotal api responses");
            Cache.SaveIpInfoes();
        }
    }
}
