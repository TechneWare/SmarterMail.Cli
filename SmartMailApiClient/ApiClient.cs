using Newtonsoft.Json;
using SmartMailApiClient.Models;
using SmartMailApiClient.Models.Requests;
using SmartMailApiClient.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient
{
    public class ApiClient : IDisposable
    {

        private readonly static List<HttpMethod> noBodyVerbs = [HttpMethod.Get, HttpMethod.Options];

        private readonly HttpClient client;
        private readonly string serverAddress;
        private readonly HttpProtocol httpProtocol;
        private bool disposedValue;

        public Session? Session { get; set; } = null;

        public ApiClient(HttpProtocol httpProtocol, string serverAddress)
        {
            this.serverAddress = serverAddress;
            this.httpProtocol = httpProtocol;
            this.client = new HttpClient();
            var version = Assembly.GetEntryAssembly()!.GetName().Version;
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"smbot/{(version)} (compatible; smarter/0.1)");
        }

        public async Task<LoginResponse> Login(string username, string password)
        {
            string apiPath = "api/v1/auth/authenticate-user";
            var loginRequest = new Models.Requests.Credential() { username = username, password = password };

            var result = await ExecuteRequest(httpMethod: HttpMethod.Post, requestUri: apiPath, requestObj: loginRequest);

            var response = JsonConvert.DeserializeObject<LoginResponse>(result.data);
            if (response == null || !response.success)
            {
                response = new LoginResponse()
                {
                    resultCode = response?.resultCode ?? 401,
                    ResponseError = result.error
                };
            }

            return response;
        }

        public async Task<LoginResponse> RefreshToken(string token)
        {
            string apiPath = "api/v1/auth/refresh-token";
            var refreshRequest = new Models.Requests.RefreshToken() { token = token };

            var result = await ExecuteRequest(httpMethod: HttpMethod.Post, requestUri: apiPath, requestObj: refreshRequest);

            var response = JsonConvert.DeserializeObject<LoginResponse>(result.data);
            if (response == null || !response.success)
            {
                response = new LoginResponse()
                {
                    resultCode = response?.resultCode ?? 401,
                    ResponseError = result.error
                };
            }

            return response;
        }

        public async Task<TempBlockedIPCountsResponse> GetCurrentlyBlockedIPsCount()
        {
            string apiPath = "api/v1/settings/sysadmin/blocked-ips-count";
            var request = new Models.Requests.GetTempBlockedIPs()
            {
                serviceTypes =
               [
                   ServiceType.Smtp,
                   ServiceType.Imap,
                   ServiceType.Pop,
                   ServiceType.Delivery,
                   ServiceType.Ldap,
                   ServiceType.EmailHarvesting,
                   ServiceType.GreyListLegacy,
                   ServiceType.Xmpp,
                   ServiceType.WebMail,
                   ServiceType.ActiveSync,
                   ServiceType.MapiEws,
                   ServiceType.AuthUsers
               ],
                search = "",
                sortType = IpBlockInfoSortType.timeLeft,
                ascending = true,
                startindex = 0,
                count = 1000
            };

            var result = await ExecuteRequest(requiresAccessToken: true,
                httpMethod: HttpMethod.Post,
                requestUri: apiPath,
                requestObj: request);

            var response = JsonConvert.DeserializeObject<TempBlockedIPCountsResponse>(result.data);
            if (response == null || !response.success)
            {
                response = new TempBlockedIPCountsResponse()
                {
                    ResponseError = result.error
                };
            }

            return response;
        }

        public async Task<TempBlockedIPsResponse> GetCurrentlyBlockedIPs()
        {
            string apiPath = "api/v1/settings/sysadmin/blocked-ips";
            var request = new Models.Requests.GetTempBlockedIPs()
            {
                serviceTypes =
               [
                   ServiceType.Smtp,
                   ServiceType.Imap,
                   ServiceType.Pop,
                   ServiceType.Delivery,
                   ServiceType.Ldap,
                   ServiceType.EmailHarvesting,
                   ServiceType.GreyListLegacy,
                   ServiceType.Xmpp,
                   ServiceType.WebMail,
                   ServiceType.ActiveSync,
                   ServiceType.MapiEws,
                   ServiceType.AuthUsers
               ],
                search = "",
                sortType = IpBlockInfoSortType.ip,
                ascending = true,
                startindex = 0,
                count = 1000
            };

            var result = await ExecuteRequest(requiresAccessToken: true,
                httpMethod: HttpMethod.Post,
                requestUri: apiPath,
                requestObj: request);

            var response = JsonConvert.DeserializeObject<TempBlockedIPsResponse>(result.data);
            if (response == null || !response.success)
            {
                response = new TempBlockedIPsResponse()
                {
                    ResponseError = result.error
                };
            }

            return response;
        }

        public async Task<PermaBlockedIpsResponse> GetPermaBlockedIPs(int pageSize, int skip)
        {
            string apiPath = "/api/v1/settings/sysadmin/ip-access/false";
            var request = new Models.Requests.GetPermaBlockedIps()
            {
               showHoneypot = false,
               searchParams = new Models.Requests.SearchParams()
               {
                   skip = skip,
                   take = pageSize,
                   search = "",
                   sortField = "ip",
                   sortDescending = false
               }
            };

            var result = await ExecuteRequest(requiresAccessToken: true,
                httpMethod: HttpMethod.Post,
                requestUri: apiPath,
                requestObj: request);

            var response = JsonConvert.DeserializeObject<PermaBlockedIpsResponse>(result.data);
            if (response == null || !response.success)
            {
                response = new PermaBlockedIpsResponse()
                {
                    ResponseError = result.error
                };
            }

            return response;
        }

        public async Task<DeletePermaBlockedIpResponse> DeletePermaBlockedIP(string ipAddress)
        {
            string apiPath = "/api/v1/settings/sysadmin/ip-access-delete";
            var request = new Models.Requests.DeletePermaBlockedIp()
            {
                address = ipAddress
            };

            var result = await ExecuteRequest(requiresAccessToken: true,
                httpMethod: HttpMethod.Post,
                requestUri: apiPath,
                requestObj: request);

            var response = JsonConvert.DeserializeObject<DeletePermaBlockedIpResponse>(result.data);
            if (response == null || !response.success)
            {
                response = new DeletePermaBlockedIpResponse()
                {
                    ResponseError = result.error
                };
            }

            return response;
        }

        public async Task<SavePermaBlockedIpResponse> SavePermaBlockedIP(string ipAddress, string description)
        {
            string apiPath = "/api/v1/settings/sysadmin/ip-access";
            var request = new Models.Requests.SavePermaBlockedIp()
            {
                serviceList = [ 
                        ServiceType.Smtp,
                        ServiceType.Imap,
                        ServiceType.Pop,
                        ServiceType.Xmpp,
                        ServiceType.WebMail
                    ],
                address = ipAddress,
                oldAddress = ipAddress,
                dataType = IPAccessDataType.BlackList,
                description = description,
                spam_bypass = false
            };

            var result = await ExecuteRequest(requiresAccessToken: true,
                httpMethod: HttpMethod.Post,
                requestUri: apiPath,
                requestObj: request);

            var response = JsonConvert.DeserializeObject<SavePermaBlockedIpResponse>(result.data);
            if (response == null || !response.success)
            {
                response = new SavePermaBlockedIpResponse()
                {
                    ResponseError = result.error
                };
            }

            return response;
        }

        public async Task<DeleteTempBlockedIpResponse> DeleteTempBlockedIP(IpTempBlock tempBlock)
        {
            string apiPath = "/api/v1/settings/sysadmin/unblock-ips";
            var request = new Models.Requests.DeleteTempBlockedIp()
            {
                ipBlocks = [ tempBlock ]
            };

            var result = await ExecuteRequest(requiresAccessToken: true,
                httpMethod: HttpMethod.Post,
                requestUri: apiPath,
                requestObj: request);

            var response = JsonConvert.DeserializeObject<DeleteTempBlockedIpResponse>(result.data);
            if (response == null || !response.success)
            {
                response = new DeleteTempBlockedIpResponse()
                {
                    ResponseError = result.error
                };
            }

            return response;
        }

        private async Task<(string data, Error? error)> ExecuteRequest(HttpMethod httpMethod, string requestUri, object? requestObj = null, bool requiresAccessToken = false)
        {
            string result = "";
            Error? resultError = null;

            string requestUrl = $"{httpProtocol}://{serverAddress}/{requestUri}";

            try
            {
                var msg = new HttpRequestMessage(httpMethod, requestUrl);
                msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!noBodyVerbs.Contains(httpMethod))
                {
                    msg.Content = new StringContent(JsonConvert.SerializeObject(requestObj), Encoding.UTF8);
                    msg.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }

                if (requiresAccessToken)
                {
                    //Login and Refresh do not require an access token, so they should never trigger this.

                    if (Session?.AccessTokenExpiration.AddMinutes(-1) < DateTime.UtcNow)
                    {
                        //If caller is not maintaining the token refresh interval, insure it gets refreshed
                        //Can fire up to 1 min prior to actual expire time or when next request is made
                        var refreshResponse = await RefreshToken(Session.RefreshToken);
                        if (refreshResponse != null && refreshResponse.success)
                        {
                            Session.AccessToken = refreshResponse.accessToken;
                            Session.AccessTokenExpiration = refreshResponse.accessTokenExpiration ?? DateTime.Now;
                            Session.RefreshToken = refreshResponse.refreshToken;
                            Session.CanViewPasswords = refreshResponse.canViewPasswords;
                        }
                    }
                    msg.Headers.Add(HttpRequestHeader.Authorization.ToString(), $"Bearer {Session?.AccessToken}");
                }

                var response = await client.SendAsync(msg);

                if (response.IsSuccessStatusCode)
                {
                    result = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    resultError = new Error(response.StatusCode, $"Error from Uri[{requestUrl}]", requestedObj: requestObj);
                }
            }
            catch (Exception ex)
            {
                resultError = new Error(HttpStatusCode.BadRequest, $"Unknown Error from Uri[{requestUrl}] {ex.Message}", requestedObj: requestObj);
            }

            return (result, resultError);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
