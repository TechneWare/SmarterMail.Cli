using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Removes an IP in the temporary block list (IDS) 
    /// </summary>
    public class DeleteTempBlockedIpCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string ipAddress;

        public string CommandName => "DeleteTemp";

        public string CommandArgs => "IpAddress";

        public string[] CommandAlternates => ["dt"];

        public string Description => "Removes an IP from the temporary block list";

        public string ExtendedDescription => "";

        public DeleteTempBlockedIpCommand()
            : base(Globals.Logger)
        {

        }

        public DeleteTempBlockedIpCommand(string IpAddress)
            : base(Globals.Logger)
        {
            ipAddress = IpAddress;
        }


        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new DeleteTempBlockedIpCommand();
            else if (args.Length != 2)
                return new BadCommand("Invalid Syntax - Use EG:DeleteTemp 127.0.0.1 or dt 127.0.0.1");

            return new DeleteTempBlockedIpCommand(args[1]);
        }

        public void Run()
        {
            if (IsConnectionOk(Globals.ApiClient))
            {
                Log.Info($"Deleting Temp Block on [{this.ipAddress}]");

                var allTempBlocksResponse = Globals.ApiClient?.GetCurrentlyBlockedIPs().ConfigureAwait(false).GetAwaiter().GetResult();
                if (IsResponseOk(allTempBlocksResponse))
                {
                    var thisTempBlock = allTempBlocksResponse!.ipBlocks?.Where(i => i.ip == this.ipAddress).FirstOrDefault();
                    if (thisTempBlock != null)
                    {
                        var r = Globals.ApiClient?.DeleteTempBlockedIP(thisTempBlock).ConfigureAwait(false).GetAwaiter().GetResult();
                        if (IsResponseOk(r))
                            Log.Debug($"Done Deleting Temp Block on [{this.ipAddress}]");
                    }
                }
            }
        }
    }
}
