using SmartMail.Cli.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Schedules a new job that executes every specified number of sections
    /// </summary>
    public class ScheduleCommand : CommandBase, ICommand, ICommandFactory
    {
        private string[] args;

        public string CommandName => "Sched";

        public string CommandArgs => "#Seconds commandToRun";

        public string[] CommandAlternates => [];

        public string Description => "Schedules a job to run on an interval until stopped";

        public string ExtendedDescription => @"EG: To run a script every 30 seconds use: sched 30 run c:\temp\myscript.txt";

        public ScheduleCommand()
            : base(Globals.Logger)
        {
            args = [];
        }
        public ScheduleCommand(string[] args)
            : base(Globals.Logger)
        {
            this.args = args;
            RequiresInteractiveMode = true;
        }
        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
            {
                return new ScheduleCommand();
            }
            else if (args.Length < 3)
            {
                return new BadCommand("Arguments are invalid");
            }

            args = args.Except([args[0]]).ToArray();
            return new ScheduleCommand(args);
        }

        public void Run()
        {
            if (int.TryParse(args[0], out int seconds))
            {
                //Resolve the scheduled command
                args = args.Skip(1).ToArray();
                var availableCommands = Utils.GetAvailableCommands();
                var parser = new CommandParser(availableCommands);
                var cmd = parser.ParseCommand(this.args);

                if (cmd != null)
                {
                    //Create a new job and add it to the jobs list
                    var newJob = new Job(cmd, seconds);
                    Globals.Jobs.Add(newJob);
                    Log.Info($"Job #{newJob.Id} scheduled to run at {newJob.NextRun}");
                }
            }
            else
                Log.Error($"Invalid Arguments");


        }
    }
}
