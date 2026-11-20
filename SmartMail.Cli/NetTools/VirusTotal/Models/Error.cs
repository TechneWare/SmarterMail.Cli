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
        public HttpStatusCode StatusCode { get; }
        public string StatusMessage => StatusCode.ToString();
        public string? VirusTotalCode { get; init; }
        public object? RequestedObj { get; internal set; }

        public Error(HttpStatusCode httpStatusCode, string message, object? requestedObj = null)
        {
            StatusCode = httpStatusCode;
            this.Message = message;
            RequestedObj = requestedObj;
        }

        public override string ToString()
        {
            return $"Status: {StatusMessage} - {Message}";
        }
    }
}
