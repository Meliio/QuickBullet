using LiteDB;
using Microsoft.Playwright;
using QuickBullet.Blocks;
using QuickBullet.Models;
using RuriLib.Parallelization;
using Spectre.Console;
using System.Text;

namespace QuickBullet
{
    public class Checker
    {
        private readonly CheckerStats _stats;
        private readonly ConfigSettings _configSettings;
        private readonly IEnumerable<Block> _blocks;
        private readonly IEnumerable<BotInput> _botInputs;
        private readonly ProxyHttpClientManager _proxyHttpClientManager;
        private readonly bool _useProxy;
        private readonly int _skip;
        private readonly int _degreeOfParallelism;
        private readonly bool _verbose;
        private readonly QuickBulletSettings _quickBulletSettings;
        private readonly Record _record;
        private readonly IEnumerable<string> _statusesToBreak;
        private readonly IEnumerable<string> _statusesToRecheck;
        private readonly ReaderWriterLock _readerWriterLock;

        public Checker(ConfigSettings configSettings, IEnumerable<Block> blocks, IEnumerable<BotInput> botInputs, ProxyHttpClientManager proxyHttpClientManager, bool useProxy, int skip, int degreeOfParallelism, bool verbose, QuickBulletSettings quickBulletSettings, Record record)
        {
            _stats = new CheckerStats(skip);
            _configSettings = configSettings;
            _blocks = blocks;
            _botInputs = botInputs;
            _proxyHttpClientManager = proxyHttpClientManager;
            _useProxy = useProxy;
            _skip = skip;
            _degreeOfParallelism = degreeOfParallelism;
            _verbose = verbose;
            _quickBulletSettings = quickBulletSettings;
            _record = record;
            _statusesToBreak = new string[] { "toCheck", "failure", "retry", "ban", "error" };
            _statusesToRecheck = new string[] { "retry", "ban", "error" };
            _readerWriterLock = new ReaderWriterLock();
        }

        public async Task StartAsync()
        {
            using var httpClient = new HttpClient(new HttpClientHandler() 
            { 
                UseCookies = false 
            });

            using var playwright = await Playwright.CreateAsync();

            Func<BotInput, CancellationToken, Task<bool>> check = new(async (input, cancellationToken) =>
            {
                BotData botData = null;

                while (true)
                {
                    var proxyHttpClient = _proxyHttpClientManager.GetRandomProxyHttpClient();

                    botData = new BotData(_quickBulletSettings, input, httpClient, proxyHttpClient, playwright)
                    {
                        UseProxy = _useProxy
                    };

                    botData.Variables.Add("data.proxy", proxyHttpClient.Proxy is null ? string.Empty : proxyHttpClient.Proxy.ToString());

                    foreach (var customInput in _configSettings.CustomInputs)
                    {
                        botData.Variables.Add(customInput.Name, customInput.Value);
                    }

                    foreach (var block in _blocks)
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
                            else
                            {
                                if (_verbose)
                                {
                                    AnsiConsole.WriteException(error);
                                }
                            }
                            botData.Variables["botStatus"] = "retry";
                        }

                        if (_statusesToBreak.Contains(botData.Variables["botStatus"], StringComparer.OrdinalIgnoreCase))
                        {
                            break;
                        }
                    }

                    await botData.DisposeAsync();

                    if (_statusesToRecheck.Contains(botData.Variables["botStatus"], StringComparer.OrdinalIgnoreCase))
                    {
                        if (botData.Variables["botStatus"].Equals("ban", StringComparison.OrdinalIgnoreCase))
                        {
                            proxyHttpClient.IsValid = false;
                        }
                        _stats.Increment(botData.Variables["botStatus"]);
                    }
                    else
                    {
                        break;
                    }
                }

                var botStatus = botData.Variables["botStatus"].ToLower();

                if (botStatus.Equals("failure"))
                {
                    _stats.Increment(botData.Variables["botStatus"]);
                    _stats.Increment("checked");
                    return true;
                }

                var outputPath = Path.Combine(_quickBulletSettings.OutputDirectory, _configSettings.Name, $"{botStatus}.txt");
                var output = OutputBuilder(botData);

                await AppendOutputToFileAsync(outputPath, output);

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

                _stats.Increment(botStatus);
                _stats.Increment("checked");

                return true;
            });

            var parallelizer = ParallelizerFactory<BotInput, bool>.Create(type: ParallelizerType.TaskBased, workItems: _botInputs, workFunction: check, degreeOfParallelism: _degreeOfParallelism, totalAmount: _botInputs.Skip(_skip).Count(), skip: _skip);

            await parallelizer.Start();

            AnsiConsole.MarkupLine($"[grey]LOG:[/] started at {parallelizer.StartTime}");

            _ = StartUpdatingConsoleCheckerStatsAsync(parallelizer);

            _ = StartUpdatingRecordAsync();

            if (_useProxy)
            {
                _ = _proxyHttpClientManager.StartValidateAllProxiesAsync();
            }

            await parallelizer.WaitCompletion();

            AnsiConsole.MarkupLine($"[grey]LOG:[/] completed at {parallelizer.EndTime}");
        }

        private string OutputBuilder(BotData botData) => botData.Captures.Any() ? new StringBuilder().Append(botData.Input.ToString()).Append(_quickBulletSettings.OutputSeparator).AppendJoin(_quickBulletSettings.OutputSeparator, botData.Captures.Select(c => $"{c.Key} = {c.Value}")).ToString() : botData.Input.ToString();

        private async Task AppendOutputToFileAsync(string path, string content)
        {
            try
            {
                _readerWriterLock.AcquireWriterLock(int.MaxValue);
                using var streamWriter = File.AppendText(path);
                await streamWriter.WriteLineAsync(content);
            }
            finally
            {
                _readerWriterLock.ReleaseWriterLock();
            }
        }

        private async Task StartUpdatingConsoleCheckerStatsAsync(Parallelizer<BotInput, bool> _parallelizer)
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            var checkerStats = new StringBuilder();

            while (true)
            {
                checkerStats
                    .Append((int)_parallelizer.Progress)
                    .Append("% Success: ")
                    .Append(_stats.Success)
                    .Append(" Custom: ")
                    .Append(_stats.Custom)
                    .Append(" Failure: ")
                    .Append(_stats.Failure)
                    .Append(" ToCheck: ")
                    .Append(_stats.ToCheck)
                    .Append(" Retry: ")
                    .Append(_stats.Retry)
                    .Append(" Ban: ")
                    .Append(_stats.Ban)
                    .Append(" Error: ")
                    .Append(_stats.Error)
                    .Append(" CPM: ")
                    .Append(_parallelizer.CPM)
                    .Append(" | ")
                    .Append(_parallelizer.Elapsed);

                Console.Title = checkerStats.ToString();

                checkerStats.Clear();

                await periodicTimer.WaitForNextTickAsync();
            }
        }

        private async Task StartUpdatingRecordAsync()
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            using var database = new LiteDatabase("Kraken.db");

            var collection = database.GetCollection<Record>("records");

            while (true)
            {
                _record.Progress = _stats.Progress;

                collection.Update(_record);

                await periodicTimer.WaitForNextTickAsync();
            }
        }
    }
}
