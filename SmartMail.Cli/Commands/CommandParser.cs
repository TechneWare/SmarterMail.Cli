using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Parses an array of arguments and translates them into commands
    /// </summary>
    internal class CommandParser
    {
        private readonly IEnumerable<ICommandFactory> commands;      //The commands this parser knows about

        /// <summary>
        /// Initilizes the parser with a list of commands
        /// </summary>
        /// <param name="commands">The commands this parser will use</param>
        public CommandParser(IEnumerable<ICommandFactory> commands)
        {
            this.commands = commands;
        }

        /// <summary>
        /// Parses an array of arguments
        /// </summary>
        /// <param name="args">the arguments to parse</param>
        /// <returns>An ICommand ready to run</returns>
        internal ICommand ParseCommand(string[] args)
        {
            //Args should always contain at least the command that was requested
            if (args.Length == 0)
                return new BadCommand("Attempt to parse an empty command");

            var requestedCommand = args[0]; //Args 0 is always the requested command

            var command = FindRequestedCommand(requestedCommand);
            if (command == null)
                return new NotFoundCommand(commandName: requestedCommand);

            if (command is BadCommand)
                return (ICommand)command;

            return command.MakeCommand(args);
        }

        /// <summary>
        /// Finds the requested command if possible
        /// </summary>
        /// <param name="requestedCommand">The string command to find</param>
        /// <returns>an ICommandFactory for the resolved command</returns>
        private ICommandFactory FindRequestedCommand(string requestedCommand)
        {
            ICommandFactory? cmd = null;

            //Match if the request matches any alternate in full
            //Otherwise look for commands with a name that starts with the request
            var matched = commands.Where(c => c.CommandAlternates.Any(ca => ca.Equals(requestedCommand, StringComparison.CurrentCultureIgnoreCase)));
            if (!matched.Any())
                matched = commands.Where(c => c.CommandName.StartsWith(requestedCommand, StringComparison.CurrentCultureIgnoreCase));

            if (matched.Any())
            {
                if (matched.Count() == 1)
                    cmd = matched.First();  //Only one match, so its safe to use it
                else
                {
                    //More than one match so return a bad command to indicate multiple matches
                    var msg = "Please Be More Specific." +
                              "\nDid you mean one of these?";
                    foreach (var c in matched)
                        msg += $"\n{c.CommandName.PadRight(25)}-{c.Description}";

                    cmd = new BadCommand(msg);
                }
            }

            return cmd;
        }
    }
}
