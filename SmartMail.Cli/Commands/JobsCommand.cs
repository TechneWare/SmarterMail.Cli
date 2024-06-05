using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Lists all currently scheduled jobs
    /// </summary>
    public class JobsCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "Jobs";

        public string CommandArgs => "";

        public string[] CommandAlternates => [];

        public string Description => "Displays any running jobs";

        public string ExtendedDescription => "";

        public JobsCommand()
            : base(Globals.Logger)
        {

        }

        public ICommand MakeCommand(string[] args)
        {
            return new JobsCommand();
        }

        public void Run()
        {
            Log.Info("---- Running Jobs ----");
            foreach (var job in Globals.Jobs)
            {
                Log.Info($"Command[{job.Command}]ID: {job.Id.ToString().PadRight(5)}#Executions:{job.NumExecutions.ToString().PadRight(5)}Next Run:{job.NextRun}");
            }
            Log.Info("----------------------");
        }
    }
}
