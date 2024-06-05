using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli.Commands
{
    /// <summary>
    /// Refreshes the auth tokens with a SmarterMail server
    /// </summary>
    internal class RefreshTokenCommand : CommandBase, ICommand, ICommandFactory
    {
        public string CommandName => "refresh";

        public string CommandArgs => "";

        public string[] CommandAlternates => ["rt"];

        public string Description => "Refreshes the current login tokens";

        public string ExtendedDescription => "";

        public RefreshTokenCommand()
            : base(Globals.Logger)
        { }

        public ICommand MakeCommand(string[] args)
        {
            return new RefreshTokenCommand();
        }

        public void Run()
        {
            if (IsConnectionOk(Globals.ApiClient))
            {
                var r = Globals.ApiClient!.RefreshToken(Globals.Session.RefreshToken).ConfigureAwait(false).GetAwaiter().GetResult();

                if (IsResponseOk(r))
                {
                    //Update the token data
                    Globals.Session.AccessToken = r.accessToken;
                    Globals.Session.AccessTokenExpiration = r.accessTokenExpiration ?? DateTime.Now;
                    Globals.Session.RefreshToken = r.refreshToken;
                    Globals.Session.CanViewPasswords = r.canViewPasswords;

                    Log.Debug("Token Refreshed");

                    if (Globals.Settings.UseAutoTokenRefresh)
                    {
                        //Reset the timer to run this command again
                        Globals.UpdateResetInterval();

                        if (Globals.RefreshTimer != null)
                        {
                            //A timer must exist at this point, since it was created during login
                            var refreshTimespan = TimeSpan.FromSeconds(Globals.TokenRefreshSeconds);
                            Globals.RefreshTimer.Change(refreshTimespan, Timeout.InfiniteTimeSpan);
                            if (Log.LogLevel == ICommandLogger.LogLevelType.Debug)
                                Utils.ShowDefaultPrompt();
                        }
                    }
                }
            }
        }
    }
}
