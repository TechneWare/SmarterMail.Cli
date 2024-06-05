using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Exposes the Run method on all commands. All commands are of this type
    /// </summary>
    public interface ICommand
    {
        void Run();
    }
}
