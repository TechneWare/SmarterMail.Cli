using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Responses
{
    public interface IResponse
    {
        public bool success { get; set; }
        public Error? ResponseError { get; set; }
    }
}
