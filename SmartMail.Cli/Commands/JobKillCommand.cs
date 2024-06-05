using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Kills a currently scheduled job
    /// </summary>
    public class JobKillCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string[] args;

        public string CommandName => "Kill";

        public string CommandArgs => "JobId";

        public string[] CommandAlternates => [];

        public string Description => "Kills the job with Id = JobId";

        public string ExtendedDescription => "EG: Kill 1 - kills job ID #1";

        public JobKillCommand()
            : base(Globals.Logger)
        {

        }
        public JobKillCommand(string[] args)
            : base(Globals.Logger)
        {
            this.args = args;
        }
        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new JobKillCommand();
            else if (args.Length != 2)
                return new BadCommand("Must be called with the ID of the job to kill");

            return new JobKillCommand(args.Skip(1).ToArray());
        }

        public void Run()
        {
            if (int.TryParse(args[0], out var jobId))
            {
                var job = Globals.Jobs.Where(j => j.Id == jobId).FirstOrDefault();
                if (job != null)
                {
                    Globals.Jobs.Remove(job);
                    Log.Info($"Job #{job.Id} was stopped");
                    job.Dispose();
                }
                else
                    Log.Warning($"JobID {jobId} was not found");
            }
            else
                Log.Error($"Invalid ID: {args[0]}");
        }
    }
}
