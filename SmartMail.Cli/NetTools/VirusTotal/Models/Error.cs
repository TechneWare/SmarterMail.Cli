using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.NetTools.Models
{
    public class Error
    {
        public string Message { get; internal set; }
        public string StatusMessage { get; internal set; }
        public object? RequestedObj { get; internal set; }

        public Error(HttpStatusCode httpStatusCode, string message, object? requestedObj = null)
        {
            this.StatusMessage = httpStatusCode.ToString();
            this.Message = message;
            RequestedObj = requestedObj;
        }

        public override string ToString()
        {
            return $"Status: {StatusMessage} - {Message}";
        }
    }
}
