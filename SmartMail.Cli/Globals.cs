using SmartMail.Cli.Commands;
using SmartMailApiClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SmartMail.Cli
{
    /// <summary>
    /// Holds configuration and status variables for various parts of the system
    /// </summary>
    internal static class Globals
    {
        public static Settings Settings { get; set; }
        public static ICommandLogger Logger { get; set; }

        public static Session Session { get; set; } = new Session();
        public static ApiClient? ApiClient { get; set; }

        public static int TokenRefreshSeconds { get; set; }
        public static System.Threading.Timer? RefreshTimer { get; set; }

        public static List<Models.Job> Jobs { get; set; } = [];
        public static bool IsQuitSignaled { get; set; } = false;
        public static bool IsInteractiveMode { get; set; } = false;
        public static bool ShowScriptProgress { get; set; } = false;

        // Variables to keep track of quotas for the Virus Total API
        // Free Quota is Request based: 4/min 500/day 15.5k/month
        // Good candidate for Refactoring: should probably live on the Virus Total API
        public static DateTime vtLastAccess { get; set; }
        public static DateTime vtPeriodStart { get; set; }
        public static int vtMinuteQuota { get; set; } = 4;
        public static int vtDailyQuota { get; set; } = 500;
        public static int vtMinuteAccessCount { get; set; } = 0;
        public static int vtDailyAccessCount { get; set; } = 0;

        /// <summary>
        /// Resets and records Virus Total access counts by minute and day
        /// Called whenever an API call succeeds
        /// </summary>
        public static void RecordAccessVtApi()
        {
            if ((DateTime.UtcNow - vtLastAccess).TotalMinutes >= 1)
            {
                vtMinuteAccessCount = 0;
                vtLastAccess = DateTime.UtcNow;
            }

            if ((DateTime.UtcNow - vtPeriodStart).TotalDays >= 1)
            {
                vtDailyAccessCount = 0;
                vtPeriodStart = new DateTime(vtLastAccess.Year, vtLastAccess.Month, vtLastAccess.Day);
            }

            vtMinuteAccessCount++;
            vtDailyAccessCount++;
        }

        /// <summary>
        /// Used to skip logic if a virus total quota has been exceeded
        /// </summary>
        /// <returns>T/F - if you can access the API at this time</returns>
        public static bool TryAccessVtApi()
        {
            //minute quota exceeded
            if ((DateTime.UtcNow - vtLastAccess).TotalMinutes < 1 && vtMinuteAccessCount >= vtMinuteQuota)
            {
                Logger.Warning("Minute Virus Total Quota Exceeded");
                var nextLocalPeriodMin = vtLastAccess.AddMinutes(1).ToLocalTime();
                LogQuota($"Denied until UTC[{vtLastAccess.AddMinutes(1)}] Local[{nextLocalPeriodMin.ToShortDateString()} {nextLocalPeriodMin.ToShortTimeString()}]");
                return false;
            }

            //Day quota exceeded
            if ((DateTime.UtcNow - vtPeriodStart).TotalDays < 1 && vtDailyAccessCount >= vtDailyQuota)
            {
                Logger.Warning("Daily Virus Total Quota Exceeded");
                var nextPeriodStart = vtPeriodStart.AddDays(1).Date;
                var nextLocalDay = nextPeriodStart.ToLocalTime();
                LogQuota($"Denied until UTC[{nextPeriodStart}] Local[{nextLocalDay.ToShortDateString()} {nextLocalDay.ToShortTimeString()}]");
                return false;
            }

            LogQuota("Allowed");
            return true;
        }
        private static void LogQuota(string AccessStatus)
        {
            Logger.Info($"Virus Total Quota: Daily[{vtDailyAccessCount}/{vtDailyQuota}] Minute[{vtMinuteAccessCount}/{vtMinuteQuota}]");
            Logger.Info($"Virus Total Access is {AccessStatus}");
        }

        /// <summary>
        /// Sets the daily Virus Total counter to max, to stop further failed requests
        /// Called if a response from Virus Total indicates exceeded quota
        /// </summary>
        public static void ExpireAccessVtApi()
        {
            vtDailyAccessCount = vtDailyQuota;
        }

        static Globals()
        {
            //Gets settings from config.json
            Settings = Settings.GetSettings();

            //Configures the logger
            Logger = new CommandLogger()
                .UseTimestamps()
                .UseLogLevel();
            Logger.SetLogLevel(Settings.LoggingLevel);

            //Initialize the Virus Total counters
            vtLastAccess = DateTime.UtcNow;
            vtPeriodStart = new DateTime(vtLastAccess.Year, vtLastAccess.Month, vtLastAccess.Day); //appears to reset quota at midnight UTC
            vtMinuteAccessCount = 0;
        }

        /// <summary>
        /// Resets the auth token refresh interval
        /// </summary>
        public static void UpdateResetInterval()
        {
            //Target a refresh no less than 5 minutes from expiration
            var expireTimespan = Session.AccessTokenExpiration.AddMinutes(-5) - DateTime.UtcNow;
            if (expireTimespan.TotalSeconds > 0)
                TokenRefreshSeconds = (int)expireTimespan.TotalSeconds;
            else
                TokenRefreshSeconds = 30;

            Logger.Debug($"Token Refresh Seconds: {TokenRefreshSeconds}");
        }
    }

}
