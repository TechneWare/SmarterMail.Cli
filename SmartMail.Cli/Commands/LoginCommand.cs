using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Performs a login against a SmarterMail server
    /// </summary>
    public class LoginCommand : CommandBase, ICommand, ICommandFactory
    {
        private readonly string userName;
        private readonly string password;

        public string CommandName => "Login";

        public string CommandArgs => "UserName Password";

        public string[] CommandAlternates => [];

        public string Description => "Logs into the API";

        public string ExtendedDescription => "Logs into the API, retreiving auth tokens for the current session";

        public LoginCommand()
            : base(Globals.Logger)
        { }

        public LoginCommand(string userName, string password)
            : base(Globals.Logger)
        {
            this.userName = userName;
            this.password = password;
        }
        public ICommand MakeCommand(string[] args)
        {
            if (args.Length <= 1)
            {
                return new LoginCommand();
            }
            else if (args.Length != 3)
            {
                return new BadCommand("must supply username and password");
            }

            return new LoginCommand(args[1], args[2]);
        }

        public void Run()
        {
            //Set a new api client onto the globals object
            Globals.ApiClient = new SmartMailApiClient.ApiClient(Globals.Settings.Protocol, Globals.Settings.ServerAddress);
            
            var r = Globals.ApiClient.Login(userName, password).ConfigureAwait(false).GetAwaiter().GetResult();
            if (IsResponseOk(r))
            {
                //Remember the session data
                Globals.Session = new SmartMailApiClient.Session()
                {
                    UserName = r.username,
                    EmailAddress = r.emailAddress,
                    AccessToken = r.accessToken,
                    AccessTokenExpiration = r.accessTokenExpiration ?? DateTime.Now,
                    RefreshToken = r.refreshToken,
                    IsAdmin = r.isAdmin,
                    IsDomainAdmin = r.isDomainAdmin,
                    CanViewPasswords = r.canViewPasswords,
                    IsImpersonating = r.isImpersonating,
                    IsLicenseValid = r.isLicensed,
                    LocaleId = r.localeId,
                    RequiresChangePassword = r.changePasswordNeeded
                };
                Globals.ApiClient.Session = Globals.Session;

                // If using auto refresh, (passive refresh also works) 
                // Setup a timer to execute the token refresh request in the background
                if (Globals.Settings.UseAutoTokenRefresh)
                {
                    Globals.UpdateResetInterval();

                    //Start over if they log in again
                    if (Globals.RefreshTimer != null)
                        Globals.RefreshTimer.Dispose();

                    var refreshTimespan = TimeSpan.FromSeconds(Globals.TokenRefreshSeconds);
                    Globals.RefreshTimer = new Timer((object? stateInfo) =>
                    {
                        var refreshToken = new RefreshTokenCommand().MakeCommand([]);
                        refreshToken.Run();

                    }, new AutoResetEvent(false), refreshTimespan, Timeout.InfiniteTimeSpan);
                }

                Log.Info("Logged In Successfully");
            }
        }
    }
}
