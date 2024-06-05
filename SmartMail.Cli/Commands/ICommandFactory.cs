using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Allows for the creation of commands with their configuration pre-set
    /// </summary>
    internal interface ICommandFactory
    {
        string CommandName { get; }
        string CommandArgs { get; }
        string[] CommandAlternates { get; }
        string Description { get; }
        string ExtendedDescription { get; }
        ICommand MakeCommand(string[] args);
    }
}
