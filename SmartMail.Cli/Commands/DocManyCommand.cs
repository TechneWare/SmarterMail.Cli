using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Documents 1 or more IP addresses by requesting their data from Virus Total and updating the IP's description on the server
    /// </summary>
    public class DocManyCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly int number;

        public string CommandName => "DMany";

        public string CommandArgs => "number";

        public string[] CommandAlternates => ["dm", "docmany"];

        public string Description => "Documents 1 or more undocumented perma blocked IPs";

        public string ExtendedDescription => "EG: DocMany 4 //Documents up to 4 Undocumented IPs";

        public DocManyCommand()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            this.number = 0;
        }
        public DocManyCommand(int number)
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            this.number = number;
        }

        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new DocManyCommand();
            else
            {
                if (!int.TryParse(args[1], out int num))
                    return new BadCommand($"{args[1]} is not a number");
                else
                    return new DocManyCommand(num);
            }
        }

        public void Run()
        {
            if (Globals.TryAccessVtApi())
            {
                //make sure cache is loaded first
                if (!Cache.IsValid)
                {
                    var loadCacheScript = new Script(Log, ["load"], "LoadCache");
                    loadCacheScript.Parse();
                    loadCacheScript.Run();
                }

                var undocumented = Cache.AllBlockedIps
                    .Where(i => !i.IsTemporary && !i.IsDocumented)
                    .Take(number)
                    .ToList();

                if (undocumented.Count != 0)
                {

                    Log.Info($"Found {undocumented.Count} IPs to document");

                    var docScript = new Script(Log, "DocManyIps");
                    var curLogLevel = Log.LogLevel;
                    Log.SetLogLevel(ICommandLogger.LogLevelType.Warning);

                    foreach (var ip in undocumented)
                    {
                        Log.Prompt($"\nDocumenting {ip.Ip} {ip.Description}");
                        docScript.Add($"doc {ip.Ip} {ip.Protocol} noload nosave");
                    }

                    docScript.Add("SaveIpInfo"); //Persist the data retreived at the end
                    docScript.Add("InvalidateCache");  //invalidate the cache when done

                    Log.Prompt("\nRunning");
                    docScript.Run(".");
                    Log.Prompt("Done\n");

                    Log.SetLogLevel(curLogLevel);
                }
                else if (undocumented.Count == 0 && Cache.AllBlockedIps.Any(i => !i.IsTemporary))
                    Log.Info("All Perma Banned IPs are documented");
                else
                    Log.Info("Nothing to Document. Try running 'load' first.");
            }
        }
    }
}
