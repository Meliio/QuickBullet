using Microsoft.Playwright;
using Newtonsoft.Json;
using QuickBullet.Models;
using RuriLib.Parallelization;
using Spectre.Console;
using System.Net;
using System.Text;
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

        public async Task<Checker> BuildAsync()
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

            var proxyHttpClients = _proxies.Any() ? new List<ProxyHttpClient>(_proxies.Select(p => BuildProxy(p, _proxyType)).Select(p => new ProxyHttpClient(new HttpClientHandler() { UseCookies = false, Proxy = p }, p) { Timeout = TimeSpan.FromSeconds(15) })) : new List<ProxyHttpClient>() { new ProxyHttpClient(new HttpClientHandler() { UseCookies = false }, null) { Timeout = TimeSpan.FromSeconds(15) } };

            var proxyHttpClientManager = new ProxyHttpClientManager(proxyHttpClients);

            if (useProxy)
            {
                _ = proxyHttpClientManager.StartValidateAllProxiesAsync();
            }

            var record = GetRecord(configSettings.Name);

            Directory.CreateDirectory(Path.Combine(quickBulletSettings.OutputDirectory, configSettings.Name));

            var skip = _skip == -1 ? record.Progress : _skip;

            var checkerStats = new CheckerStats(skip)
            { 
                DegreeOfParallelism = _bots
            };

            var statusesToBreak = new string[] { "toCheck", "failure", "retry", "ban", "error" };
            var statusesToRecheck = new string[] { "retry", "ban", "error" };

            var readerWriterLock = new ReaderWriterLock();

            var handler = new HttpClientHandler()
            {
                UseCookies = false
            };

            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            var playwright = await Playwright.CreateAsync();

            Func<BotInput, CancellationToken, Task<bool>> check = new(async (input, cancellationToken) =>
            {
                BotData botData = null;

                for (var attempts = 0; attempts < 8; attempts++)
                {
                    var proxyHttpClient = proxyHttpClientManager.GetRandomProxyHttpClient();

                    botData = new BotData(quickBulletSettings, input, httpClient, proxyHttpClient, playwright)
                    {
                        UseProxy = useProxy
                    };

                    botData.Variables.Add("data.proxy", proxyHttpClient.Proxy is null ? string.Empty : proxyHttpClient.Proxy.ToString());

                    foreach (var customInput in configSettings.CustomInputs)
                    {
                        botData.Variables.Add(customInput.Name, customInput.Value);
                    }

                    foreach (var block in blocks)
                    {
                        try
                        {
                            await block.RunAsync(botData);
                        }
                        catch (HttpRequestException)
                        {
                            proxyHttpClient.IsValid = false;
                            botData.Variables["botStatus"] = "retry";
                        }
                        catch (Exception error)
                        {
                            if (error.Message.Contains("HttpClient.Timeout") || error.Message.Contains("ERR_TIMED_OUT"))
                            {
                                proxyHttpClient.IsValid = false;
                            }
                            else if (_verbose)
                            {
                                AnsiConsole.WriteException(error);
                            }
                            botData.Variables["botStatus"] = "retry";
                        }

                        if (statusesToBreak.Contains(botData.Variables["botStatus"], StringComparer.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }

                    await botData.DisposeAsync();

                    if (statusesToRecheck.Contains(botData.Variables["botStatus"], StringComparer.OrdinalIgnoreCase))
                    {
                        if (botData.Variables["botStatus"].Equals("ban", StringComparison.OrdinalIgnoreCase))
                        {
                            proxyHttpClient.IsValid = false;
                        }
                        checkerStats.Increment(botData.Variables["botStatus"]);
                    }
                    else
                    {
                        break;
                    }
                }

                var botStatus = statusesToRecheck.Contains(botData.Variables["botStatus"], StringComparer.OrdinalIgnoreCase) ? "tocheck" : botData.Variables["botStatus"].ToLower();

                if (botStatus.Equals("failure"))
                {
                    checkerStats.Increment(botData.Variables["botStatus"]);
                }
                else
                {
                    var outputPath = Path.Combine(quickBulletSettings.OutputDirectory, configSettings.Name, $"{botStatus}.txt");
                    var output = botData.Captures.Any() ? new StringBuilder().Append(botData.Input.ToString()).Append(quickBulletSettings.OutputSeparator).AppendJoin(quickBulletSettings.OutputSeparator, botData.Captures.Select(c => $"{c.Key} = {c.Value}")).ToString() : botData.Input.ToString();

                    try
                    {
                        readerWriterLock.AcquireWriterLock(int.MaxValue);
                        using var streamWriter = File.AppendText(outputPath);
                        await streamWriter.WriteLineAsync(output);
                    }
                    finally
                    {
                        readerWriterLock.ReleaseWriterLock();
                    }

                    switch (botStatus)
                    {

                        case "success":
                            AnsiConsole.MarkupLine($"[green4]SUCCESS:[/] {output}");
                            break;
                        case "tocheck":
                            AnsiConsole.MarkupLine($"[cyan3]TOCHECK:[/] {output}");
                            break;
                        default:
                            AnsiConsole.MarkupLine($"[orange3]{botStatus.ToUpper()}:[/] {output}");
                            break;
                    }

                    checkerStats.Increment(botStatus);
                }

                checkerStats.Increment("checked");

                return true;
            });

            var paradllelizer = ParallelizerFactory<BotInput, bool>.Create(type: ParallelizerType.TaskBased, workItems: botInputs, workFunction: check, degreeOfParallelism: _bots, totalAmount: botInputs.Skip(skip).Count(), skip: skip);

            return new Checker(paradllelizer, checkerStats, record);
        }

        private static Models.Proxy BuildProxy(string proxy, ProxyType proxyType)
        {
            var proxySplit = proxy.Split(':');

            var proxyClient = new Models.Proxy(proxySplit[0], int.Parse(proxySplit[1]), proxyType);

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