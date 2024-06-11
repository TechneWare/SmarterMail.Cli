using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.NetTools.VirusTotal.Models
{
    public class ErrorResponse
    {
        public ErrorMessage error { get; set; } = new ErrorMessage() { code = "NA", message = "Empty Error" };
    }

    public class ErrorMessage
    {
        public string code { get; set; } = "";
        public string message { get; set; } = "";
    }
}
