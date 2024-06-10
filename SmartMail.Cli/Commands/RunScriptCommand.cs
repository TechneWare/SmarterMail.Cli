using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Runs a script stored in a file
    /// </summary>
    public class RunScriptCommand : CommandBase, ICommand, ICommandFactory
    {
        public readonly string path;

        public string CommandName => "RunScript";

        public string CommandArgs => "FullPathToScript";

        public string[] CommandAlternates => ["run", "exe"];

        public string Description => @"Executes a saved script EG: RunScript C:\temp\myscript.txt or run c:\temp\myscript.txt";

        public string ExtendedDescription => "One command per line";

        public RunScriptCommand()
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            this.path = "";
        }
        public RunScriptCommand(string path)
            : base(Globals.Logger)
        {
            IsThreadSafe = false;
            this.path = path;
        }
        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
                return new RunScriptCommand();

            var path = string.Join(" ", args.Except([args[0]]));
            if (string.IsNullOrEmpty(path))
                return new BadCommand("You must provide a path to the script");

            return new RunScriptCommand(path);
        }

        public void Run()
        {

            try
            {
                //load the script file
                var fName = new FileInfo(path).Name;
                var scriptLines = File.ReadAllLines(this.path);
                //Make a script out of it
                var script = new Models.Script(Log, fName);
                var isScriptValid = script.Load(scriptLines);

                if (isScriptValid)
                {
                    //Display the script lines
                    if (Log.LogLevel == ICommandLogger.LogLevelType.Debug)
                        script.List();
                    
                    //Execute the script
                    script.Run();
                }
                else
                    Log.Error("==> INVALID SCRIPT <==");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }
    }
}
