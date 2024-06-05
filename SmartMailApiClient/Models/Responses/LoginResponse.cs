using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMailApiClient.Models.Responses
{
    public class LoginResponse : IResponse
    {
        public string emailAddress { get; set; } = string.Empty;
        public bool changePasswordNeeded { get; set; } = false;
        public bool displayWelcomeWizard { get; set; } = false;
        public bool isAdmin { get; set; } = false;
        public bool isDomainAdmin { get; set; } = false;
        public bool isLicensed { get; set; } = false;
        public string autoLoginToken { get; set; } = string.Empty;
        public string autoLoginUrl { get; set; } = string.Empty;
        public string localeId { get; set; } = string.Empty;
        public bool isImpersonating { get; set; } = false;
        public bool canViewPasswords { get; set; } = false;
        public string accessToken { get; set; } = string.Empty;
        public string refreshToken { get; set; } = string.Empty;
        public DateTime? accessTokenExpiration { get; set; }
        public string username { get; set; } = string.Empty;
        public bool success { get; set; } = false;
        public int resultCode { get; set; } = -1;

        public Error? ResponseError { get; set; }
    }
}
