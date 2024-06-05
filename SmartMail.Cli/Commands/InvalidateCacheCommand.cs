using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    public class InvalidateCacheCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "InvalidateCache";

        public string CommandArgs => "";

        public string[] CommandAlternates => [];

        public string Description => "Invalidates the current cache, signaling other commands to reload it";

        public string ExtendedDescription => "Used in scripts to make sure the cache appears invalid for follow on commands";

        public InvalidateCacheCommand()
            :base(Globals.Logger)
        {
            
        }

        public ICommand MakeCommand(string[] args)
        {
            return new InvalidateCacheCommand();
        }

        public void Run()
        {
            Cache.IsLoaded = false;
            Log.Info("Cache has been invalidated");
        }
    }
}
