using Newtonsoft.Json;
using SmartMail.Cli.Commands;
using SmartMailApiClient;
using System.Reflection;

namespace SmartMail.Cli
{
    public class Settings
    {
        private static readonly string path;
        private static readonly string configFile;
        public string VirusTotalApiKey { get; set; } = "";
        public ICommandLogger.LogLevelType LoggingLevel { get; set; } = ICommandLogger.LogLevelType.Info;
        public HttpProtocol Protocol { get; set; } = HttpProtocol.https;
        public string ServerAddress { get; set; } = "[server domain/IP address]";
        public bool UseAutoTokenRefresh { get; set; } = true;
        public double PercentAbuseTrigger { get; set; } = .001;
        public bool UseAutoLogin { get; set; } = false;
        public string AutoLoginUsername { get; set; } = "[user name]";
        public string AutoLoginPassword { get; set; } = "[password]";

        public string[] StartupScript { get; set; } = [];

        static Settings()
        {
            path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? "";
            configFile = $"{path}/config.json";
        }
        public static Settings GetSettings()
        {

            try
            {
                var configJson = "";
                if (!File.Exists(configFile))
                    return SaveSettings(new Settings());

                configJson = File.ReadAllText(configFile);
                var newSettings = JsonConvert.DeserializeObject<Settings>(configJson);

                return newSettings ?? new Settings();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Loading Settings: {ex.Message}");
                return new Settings();
            }
        }

        public static Settings SaveSettings(Settings settings)
        {
            try
            {
                var configJson = JsonConvert.SerializeObject(settings);
                var prettyJson = JsonPrettify(configJson);
                File.WriteAllText(configFile, prettyJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save settings: {ex.Message}");
            }

            return GetSettings();
        }

        public static string JsonPrettify(string json)
        {
            using var stringReader = new StringReader(json);
            using var stringWriter = new StringWriter();
            var jsonReader = new JsonTextReader(stringReader);
            var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
            jsonWriter.WriteToken(jsonReader);
            return stringWriter.ToString();
        }
    }
}