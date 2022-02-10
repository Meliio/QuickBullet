using Newtonsoft.Json;
using QuickBullet.Models;
using Spectre.Console;
using System.Net;
using System.Text.RegularExpressions;
using Yove.Proxy;

namespace QuickBullet
{
    public class CheckerBuilder
    {
        private readonly string _configFile;
        private readonly string _wordlistFile;
        private readonly IEnumerable<string> _proxies;
        private readonly ProxyType _proxyType;
        private readonly int _skip;
        private readonly int _bots;
        private readonly bool _verbose;
        private readonly Dictionary<string, Func<BotInput, InputRule, bool>> _checkInputRuleFunctions;

        private const string settingsFile = "settings.json";

        public CheckerBuilder(string configFile, string wordlistFile, IEnumerable<string> proxies, ProxyType proxyType, int skip, int bots, bool verbose)
        {
            _configFile = configFile;
            _wordlistFile = wordlistFile;
            _proxies = proxies;
            _proxyType = proxyType;
            _skip = skip;
            _bots = bots;
            _verbose = verbose;
            _checkInputRuleFunctions = new Dictionary<string, Func<BotInput, InputRule, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "input", CheckInputRule },
                { "input.user", CheckInputUsernameRule },
                { "input.pass", CheckInputPasswordRule },
                { "input.username", CheckInputUsernameRule },
                { "input.password", CheckInputPasswordRule }
            };
        }

        public Checker Build()
        {
            var quickBulletSettings = JsonConvert.DeserializeObject<QuickBulletSettings>(File.ReadAllText(settingsFile));

            var loliScriptManager = new LoliScriptManager();

            (var configSettings, var blocks) = loliScriptManager.Build(_configFile);

            if (string.IsNullOrEmpty(configSettings.Name))
            {
                configSettings.Name = Path.GetFileNameWithoutExtension(_configFile);
            }

            if (!string.IsNullOrEmpty(configSettings.AdditionalInfo))
            {
                AnsiConsole.MarkupLine($"[grey]CONFIG INFO:[/] {configSettings.AdditionalInfo}");
            }

            if (configSettings.CustomInputs.Any())
            {
                AnsiConsole.Write(new Rule("[darkorange]Custom input[/]").RuleStyle("grey").LeftAligned());

                foreach (var customInput in configSettings.CustomInputs)
                {
                    customInput.Value = AnsiConsole.Ask<string>($"{customInput.Description}:");
                }
            }

            var botInputs = File.ReadAllLines(_wordlistFile).Where(w => !string.IsNullOrEmpty(w)).Select(w => new BotInput(w));

            if (configSettings.InputRules.Any())
            {
                botInputs = botInputs.Where(b => configSettings.InputRules.All(i => _checkInputRuleFunctions[i.Name].Invoke(b, i)));
            }

            var useProxy = _proxies.Any();

            var proxyHttpClientManager = new ProxyHttpClientManager(_proxies.Select(p => BuildProxy(p, _proxyType)));

            var record = GetRecord(configSettings.Name);

            Directory.CreateDirectory(Path.Combine(quickBulletSettings.OutputDirectory, configSettings.Name));

            return new Checker(configSettings, blocks, botInputs, proxyHttpClientManager, useProxy, _skip == -1 ? record.Progress : _skip, _bots, _verbose, quickBulletSettings, record);
        }

        private static Proxy BuildProxy(string proxy, ProxyType proxyType)
        {
            var proxySplit = proxy.Split(':');

            var proxyClient = new Proxy(proxySplit[0], int.Parse(proxySplit[1]), proxyType);

            if (proxySplit.Length == 4)
            {
                proxyClient.Credentials = new NetworkCredential(proxySplit[2], proxySplit[3]);
            }

            return proxyClient;
        }

        private bool CheckInputRule(BotInput botInput, InputRule inputRule) => Regex.IsMatch(botInput.ToString(), inputRule.Regex);

        private bool CheckInputUsernameRule(BotInput botInput, InputRule inputRule) => Regex.IsMatch(botInput.Combo.Username, inputRule.Regex);

        private bool CheckInputPasswordRule(BotInput botInput, InputRule inputRule) => Regex.IsMatch(botInput.Combo.Password, inputRule.Regex);

        private Record GetRecord(string configName)
        {
            using var database = new LiteDB.LiteDatabase("Kraken.db");

            var collection = database.GetCollection<Record>("records");

            var record = collection.FindOne(r => r.ConfigName == configName && r.WordlistLocation == _wordlistFile);

            if (record is null)
            {
                record = new Record()
                {
                    ConfigName = configName,
                    WordlistLocation = _wordlistFile,
                    Progress = 0
                };

                collection.Insert(record);
            }

            return record;
        }
    }
}