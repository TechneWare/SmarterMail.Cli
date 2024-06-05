using SmartMail.Cli.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartMail.Cli.Models
{
    /// <summary>
    /// Represents a repeating scheduled task
    /// </summary>
    public class Job : IDisposable
    {
        //Maintains a unique id
        private static int _nextId = 0;
        private bool disposedValue;

        // The timer that controls execution of the job
        private Timer timer;
        // The command that runs
        private readonly Commands.ICommand command;
        // The interval in seconds between runs
        private readonly int interval;
        private TimeSpan refreshTimespan { get; set; }

        /// <summary>
        /// ID of the Job, used to Kill jobs
        /// </summary>
        public int Id { get; internal set; } = 0;
        /// <summary>
        /// Next time the job will run
        /// </summary>
        public DateTime NextRun { get; internal set; }
        /// <summary>
        /// Number of times this job has run
        /// </summary>
        public int NumExecutions { get; internal set; }
        /// <summary>
        /// The string representation of the command that will run
        /// </summary>
        public string Command => ((Commands.ICommandFactory)command).CommandName;
        /// <summary>
        /// Initilize a new job and start the timer
        /// </summary>
        /// <param name="command">The command to run</param>
        /// <param name="interval">The time in seconds between each run</param>
        public Job(Commands.ICommand command, int interval)
        {
            Id = ++_nextId;
            this.command = command;
            this.interval = interval;

            refreshTimespan = TimeSpan.FromSeconds(interval);
            this.NextRun = DateTime.UtcNow + refreshTimespan;

            //create the timer with an Inline delegate 
            timer = new Timer((object? stateInfo) =>
            {
                refreshTimespan = TimeSpan.FromSeconds(this.interval);
                this.NextRun = DateTime.UtcNow + refreshTimespan;
                var timerUpdated = this.timer!.Change(refreshTimespan, Timeout.InfiniteTimeSpan);
                if (!timerUpdated)
                    Globals.Logger.Warning($"Filed to refresh timer for job {Id}:{Command}");

                try
                {
                    command.Run();
                    this.NumExecutions++;
                }
                catch (Exception ex)
                {
                    Globals.Logger.Error($"Error running Job[{this.Id}:{Command}] - {ex.Message}");
                }
                finally
                {
                    Utils.ShowDefaultPrompt();
                }

            }, new AutoResetEvent(false), refreshTimespan, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Dispose pattern 
        /// </summary>
        /// <param name="disposing">True if this is the first call to dispose</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    timer?.Dispose();
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
