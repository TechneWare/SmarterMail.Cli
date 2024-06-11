using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    internal static class Utils
    {
        private static readonly ICommandLogger Log = Globals.Logger;
        /// <summary>
        /// Gets commands that are available to the user
        /// </summary>
        /// <returns>All command objects that are marked public</returns>
        public static IEnumerable<ICommandFactory?>? GetAvailableCommands()
        {
            var type = typeof(ICommandFactory);
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && p.IsPublic)
                .Select(t => Activator.CreateInstance(type: t) as ICommandFactory);

            return types;
        }

        /// <summary>
        /// Parses a list of commands and displays their usage
        /// </summary>
        /// <param name="availableCommands">A list of commands to process</param>
        public static void PrintUsage(IEnumerable<ICommandFactory?>? availableCommands)
        {
            if (availableCommands != null)
            {
                Log.Prompt("\nUsage: commandName [Arguments]\n");
                Log.Prompt("Commands:\n");
                foreach (var command in availableCommands)
                    if (command != null)
                        PrintCommandUsage(command);

                Log.Prompt("\n");
            }
            else
                Log.Warning("There are no commands in the system to display usage for");
        }

        /// <summary>
        /// Prints the usage for a command
        /// </summary>
        /// <param name="command">The command to document</param>
        /// <param name="showDetails">If true then show alternate commands and the extended description</param>
        public static void PrintCommandUsage(ICommandFactory command, bool showDetails = false)
        {
            string alts = "";
            string ext = "";
            if (showDetails)
            {
                if (command.CommandAlternates.Length != 0)
                    foreach (var altCommand in command.CommandAlternates)
                        alts += $" | {altCommand}";

                if (!string.IsNullOrEmpty(command.ExtendedDescription))
                    ext = $"\n{command.ExtendedDescription}";
            }
            string args = "";
            if (!string.IsNullOrEmpty(command.CommandArgs))
                args = $" [{command.CommandArgs}]";
            Log.Prompt($"\n{command.CommandName}{alts}{args}".PadRight(35));
            Log.Prompt($"- {command.Description}{ext}");
        }

        /// <summary>
        /// A simple user input, with a prompt
        /// </summary>
        /// <param name="prompt">The prompt to display to the user</param>
        /// <returns>String value of the users input</returns>
        public static string GetInput(string prompt = "$>")
        {
            Log.Prompt(prompt);
            string? commandInput = Console.ReadLine() ?? "";
            Log.LogToFile($"\nUser Input: {commandInput}\n");
            return commandInput;
        }

        public static string DefaultPrompt => $"\n{DateTime.UtcNow} UTC [{Globals.Session.UserName}]$> ";
        /// <summary>
        /// Shows the default prompt in interactive mode
        /// </summary>
        public static void ShowDefaultPrompt()
        {
            Log.Prompt(DefaultPrompt);
        }

        /// <summary>
        /// Gets input from the user and forces the user to enter a value that is in range
        /// </summary>
        /// <param name="min">The minimum valid value</param>
        /// <param name="max">The maximum valid value</param>
        /// <param name="inputFunc">A function that should be used to get the input from the user</param>
        /// <returns>Validated output as a string</returns>
        public static string InputRange(double min, double max, Func<string> inputFunc)
        {
            var isValid = false;
            var input = inputFunc();
            string result = "";

            do
            {
                if (double.TryParse(input, out double v))
                    isValid = min <= v && v <= max;

                if (!isValid)
                {
                    Log.Error($"You must enter a number between {min} and {max}");
                    input = InputRange(min, max, inputFunc);
                }

                result = v.ToString();

            } while (!isValid);


            return result;
        }

        /// <summary>
        /// Gets input from the user with a default value prompt
        /// </summary>
        /// <param name="defaultValue">The value to return if the user presses enter</param>
        /// <param name="description">The description of the input</param>
        /// <returns>string value of what the user entered</returns>
        public static string InputPrompt(string defaultValue, string description)
        {
            var value = GetInput(prompt: $"{description} ({defaultValue}): ");
            if (string.IsNullOrEmpty(value))
                value = defaultValue;

            return value;
        }
    }
}
