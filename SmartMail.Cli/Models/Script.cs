using SmartMail.Cli.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SmartMail.Cli.Models
{
    /// <summary>
    /// Builds, Parses and Runs scripts
    /// </summary>
    public class Script
    {
        private readonly ICommandLogger Log;
        private IEnumerable<ICommandFactory> availableCommands;
        private CommandParser parser;
        /// <summary>
        /// Script Name
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Lines in the script
        /// </summary>
        public string[] ScriptLines { get; internal set; }
        /// <summary>
        /// Commands in the script
        /// </summary>
        public List<ICommand> Commands { get; internal set; }

        /// <summary>
        /// Build a script from scratch
        /// </summary>
        /// <param name="Log">The Logger to use</param>
        /// <param name="name">The name of the script</param>
        public Script(ICommandLogger Log, string name = "System")
        {
            availableCommands = Utils.GetAvailableCommands();
            parser = new CommandParser(availableCommands);
            this.Log = Log;
            Name = name;
            ScriptLines = [];
            Commands = [];
            Log.Debug($"New Empty Script: [{name}] created");
        }
        /// <summary>
        /// Build a script from text lines, must call Parse after
        /// </summary>
        /// <param name="Log">The Logger to use</param>
        /// <param name="scriptLines">The lines of text in the script</param>
        /// <param name="name">The name of the script</param>
        public Script(ICommandLogger Log, string[]? scriptLines, string name = "System")
        {
            availableCommands = Utils.GetAvailableCommands();
            parser = new CommandParser(availableCommands);
            this.Log = Log;
            this.ScriptLines = scriptLines ?? [];
            Name = name;
            Commands = [];
            Log.Debug($"Script File: [{name}] loaded");
        }
        /// <summary>
        /// Build a script from command objects, cannot be parsed
        /// </summary>
        /// <param name="Log">The Logger to use</param>
        /// <param name="commands">The list of ICommand objects to run</param>
        /// <param name="name">The name of the script</param>
        public Script(ICommandLogger Log, List<ICommand> commands, string name = "System")
        {
            availableCommands = Utils.GetAvailableCommands();
            parser = new CommandParser(availableCommands);
            this.Log = Log;
            this.ScriptLines = [];
            Commands = commands;
            Name = name;
            Log.Debug($"Script Binary: [{name}] loaded");
        }
        /// <summary>
        /// Runs the script
        /// </summary>
        /// <param name="progressChar">If provided, displays the progress char as each command runs</param>
        public void Run(string progressChar = "")
        {
            try
            {
                //execute each command in the list
                Log.Debug("--- Script Started ---");
                foreach (var command in Commands)
                {
                    if (!string.IsNullOrEmpty(progressChar))
                        Log.Prompt(progressChar);

                    command.Run();

                    if (Globals.IsQuitSignaled)
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"SCRIPT FAILED: {ex.Message}");
            }
            finally
            {
                Log.Debug("--- Script Ended ---");
            }
        }
        /// <summary>
        /// Parses a script into commands from a list of text lines
        /// </summary>
        /// <returns>True if the script is valid</returns>
        public bool Parse()
        {
            var isScriptValid = false;
            Commands = [];

            try
            {
                //load the script
                ScriptLines = CleanScript(ScriptLines);

                if (ScriptLines.Length == 0)
                    throw new Exception("The script appears to be empty");
                isScriptValid = true;

                //create the command list
                foreach (var line in ScriptLines)
                {
                    isScriptValid = Add(line);
                    if (!isScriptValid)
                        break;
                }
            }
            catch (Exception ex)
            {
                isScriptValid = false;
                Log.Error(ex.Message);
            }

            return isScriptValid;
        }
        /// <summary>
        /// Loads an existing script with new script lines
        /// </summary>
        /// <param name="scriptLines">List of text script lines</param>
        /// <returns>True if the script is valid</returns>
        public bool Load(string[]? scriptLines)
        {
            var isValid = false;

            foreach (var line in CleanScript(scriptLines))
            {
                isValid = Add(line);
                if (!isValid)
                    break;
            }

            if (isValid)
                Log.Debug($"Script loaded with {ScriptLines.Length} lines");
            else
                Log.Info($"Script '{Name}' is invalid Script");

            return isValid;
        }
        /// <summary>
        /// Resets to an empty script
        /// </summary>
        public void Clear()
        {
            ScriptLines = [];
            Commands = [];
        }
        /// <summary>
        /// Lists the current script lines if any
        /// </summary>
        public void List()
        {
            Log.Prompt("\n");
            foreach (var line in ScriptLines ??= [])
                Log.Prompt($"{Array.IndexOf(ScriptLines, line)}: {line}\n");
            Log.Prompt("\n");
        }
        /// <summary>
        /// Saves the script to a file
        /// </summary>
        /// <param name="filename"></param>
        public void Save(string filename, string[] scriptHead)
        {
            try
            {
                var path = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                if(scriptHead != null && scriptHead.Any())
                {
                    ScriptLines = [.. scriptHead, .. ScriptLines];
                }
                
                File.WriteAllLines($"{path}/{filename}", ScriptLines);
                Log.Info($"Script saved to file: {path}/{filename}");
                ScriptLines = CleanScript(ScriptLines);
            }
            catch (Exception ex)
            {
                Log.Error($"Error Saving Script: {ex.Message}");
            }
        }
        /// <summary>
        /// Adds a text line to the current script if its valid
        /// </summary>
        /// <param name="line">The line of text to add to the script</param>
        /// <returns>True if the line was valid</returns>
        public bool Add(string line)
        {
            bool isValid = true;

            var args = line.Split(' ');
            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                isValid = false;
                Log.Prompt($"{Array.IndexOf(ScriptLines, line)}: {line}\n");
                Log.Warning($"SYNTAX ERROR on Line[{Array.IndexOf(ScriptLines, line)}]");
            }
            else
            {
                ICommand? command = parser.ParseCommand(args);
                if (!IsCommandValid(command, ScriptLines, line))
                {
                    isValid = false;
                    Log.Debug("==> Invalid Script <==");
                }

                ScriptLines = [.. ScriptLines, line];
                Commands.Add(command);
            }

            return isValid;
        }
        /// <summary>
        /// Cleans a script of comments and blank lines
        /// </summary>
        /// <param name="scriptLines">The script lines to clean</param>
        /// <returns>A cleaned list of script lines</returns>
        private string[] CleanScript(string[]? scriptLines)
        {
            scriptLines ??= [];
            scriptLines = scriptLines
                .Where(x => !string.IsNullOrEmpty(x)
                         && !x.StartsWith('#')).ToArray();

            return scriptLines;
        }
        /// <summary>
        /// Determines if a command object is valid and if not responds with where the problem occured 
        /// </summary>
        /// <param name="command">The command to validate</param>
        /// <param name="scriptLines">The lines of the script</param>
        /// <param name="line">The line of the script being evaluated</param>
        /// <returns>True if the command is valid</returns>
        private bool IsCommandValid(ICommand command, string[] scriptLines, string line)
        {
            var result = false;

            if (command != null)
            {
                result = true;
                if (command.GetType() == typeof(BadCommand))
                {
                    result = false;
                    Log.Prompt($"{Array.IndexOf(ScriptLines, line)}: {line}\n");
                    Log.Warning($"SYNTAX ERROR on Line[{Array.IndexOf(scriptLines, line)}]");
                    command.Run();
                }
                else if (command.GetType() == typeof(NotFoundCommand))
                {
                    result = false;
                    Log.Prompt($"{Array.IndexOf(ScriptLines, line)}: {line}\n");
                    Log.Warning($"UNKNOWN COMMAND on Line[{Array.IndexOf(scriptLines, line)}]");
                    command.Run();
                }
            }
            else
            {
                result = false;
                Log.Prompt($"{Array.IndexOf(ScriptLines, line)}: {line}\n");
                Log.Warning($"UNKNOWN COMMAND on Line[{Array.IndexOf(scriptLines, line)}]");
            }

            return result;
        }
    }
}
