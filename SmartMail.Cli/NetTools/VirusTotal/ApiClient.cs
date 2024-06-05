using Newtonsoft.Json;
using SmartMailApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SmartMailApiClient.NetTools.Models;
using SmartMail.Cli.NetTools.VirusTotal.Models;
using System.Reflection;

namespace SmartMail.Cli.NetTools.VirusTotal
{
    /// <summary>
    /// Client for Accessing Virus Total's Api
    /// </summary>
    public class ApiClient
    {
        private readonly static List<HttpMethod> noBodyVerbs = [HttpMethod.Get, HttpMethod.Options];
        private readonly HttpClient client;

        public ApiClient()
        {
            this.client = new HttpClient();
            var version = Assembly.GetEntryAssembly()!.GetName().Version;
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"smbot/{(version)} (compatible; smarter/0.1)");
        }

        /// <summary>
        /// Gets the IPAddress info from Virus Total
        /// </summary>
        /// <param name="ipAddress">The IP address being searched</param>
        /// <returns>A virus total response</returns>
        /// <exception cref="Exception">Bubbles any API errors as exceptions</exception>
        public async Task<IPAddressInfo?> GetIPAddressInfo(string ipAddress)
        {
            string uri = $"ip_addresses/{ipAddress}";

            var result = await ExecuteRequest(HttpMethod.Get, uri);
            var response = JsonConvert.DeserializeObject<IPAddressResponse>(result.data);

            if (result.error != null)
            {
                throw new Exception($"{result.error.StatusMessage}:{result.error.Message}");
            }

            return response?.data;
        }

        private async Task<(string data, Error? error)> ExecuteRequest(HttpMethod httpMethod, string requestUri, object? requestObj = null)
        {
            string result = "";
            Error? resultError = null;

            string requestUrl = $"https://www.virustotal.com/api/v3/{requestUri}";

            try
            {
                if (!Globals.TryAccessVtApi())
                    throw new Exception("Virus Total Daily Quota Exceeded");

                var msg = new HttpRequestMessage(httpMethod, requestUrl);
                msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                msg.Headers.Add("x-apikey", Globals.Settings.VirusTotalApiKey);

                if (!noBodyVerbs.Contains(httpMethod))
                {
                    msg.Content = new StringContent(JsonConvert.SerializeObject(requestObj), Encoding.UTF8);
                    msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }

                var response = await client.SendAsync(msg);
                if (response.IsSuccessStatusCode)
                {
                    Globals.RecordAccessVtApi();

                    result = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var serverResponse = await response.Content.ReadAsStringAsync();
                    var vtError = JsonConvert.DeserializeObject<ErrorResponse>(serverResponse);
                    if (vtError != null)
                        resultError = new Error(response.StatusCode, $"Error from Uri[{requestUrl}]\n{vtError.error.code}: {vtError.error.message}", requestedObj: requestObj);
                    else
                        resultError = new Error(response.StatusCode, $"Error from Uri[{requestUrl}]\nServerResponse[{serverResponse}]", requestedObj: requestObj);
                }
            }
            catch (Exception ex)
            {

                resultError = new Error(HttpStatusCode.BadRequest, $"Unknown Error from Uri[{requestUrl}] {ex.Message}", requestedObj: requestObj);
            }

            return (result, resultError);
        }

    }
}
