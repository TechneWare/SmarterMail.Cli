using SmartMailApiClient;
using SmartMailApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Base class for commands
    /// </summary>
    public abstract class CommandBase
    {
        private readonly ICommandLogger _logger;

        /// <summary>
        /// Any command that writes to shared data used by other commands should set this to false in their constructors
        /// </summary>
        public bool IsThreadSafe { get; internal set; }
        public ICommandLogger Log { get { return _logger; } }
        public bool RequiresInteractiveMode { get; set; } = false; //Assume commands can be run from the command line
        protected CommandBase(ICommandLogger logger)
        {
            this._logger = logger;
        }

        /// <summary>
        /// Commands that process API responses can use this to test if the response was OK
        /// </summary>
        /// <param name="response">The response returned from an API</param>
        /// <returns>True if the response is OK</returns>
        public bool IsResponseOk(IResponse? response)
        {
            if (response != null && response.success)
            {
                return true;
            }
            else if (response != null && response.ResponseError != null)
            {
                Log.Error($"Failed: {response.ResponseError}");
            }
            else
            {
                Log.Error("Unknown Error");
            }

            return false;
        }

        /// <summary>
        /// Commands that access the API can use this to test if the connection to the server is OK
        /// </summary>
        /// <param name="apiClient">A SmarterMail API client object</param>
        /// <returns>True if currently connected to the API, False if you need to login first</returns>
        public bool IsConnectionOk(ApiClient? apiClient)
        {
            if (apiClient != null &&
                apiClient.Session != null &&
                !string.IsNullOrEmpty(apiClient.Session.AccessToken))
            {
                return true;
            }
            else
            {
                Log.Error("You must be logged in to use this command");
                return false;
            }
        }
    }
}
