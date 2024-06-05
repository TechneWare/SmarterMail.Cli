namespace SmartMailApiClient
{
    public class Session
    {
        public string EmailAddress { get; set; } = "";
        public bool RequiresChangePassword { get; set; } = false;
        public bool IsLicenseValid { get; set; } = false;
        public string LocaleId { get; set; } = "";
        public bool IsImpersonating { get; set; } = false;
        public bool CanViewPasswords { get; set; } = false;
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public DateTime AccessTokenExpiration { get; set; } = DateTime.UtcNow;
        public string UserName { get; set; } = "";
        public bool IsAdmin { get; set; } = false;
        public bool IsDomainAdmin { get; set; } = false;
        
    }
}
