using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuickBullet.Models;
using Spectre.Console;
using Yove.Proxy;

namespace QuickBullet
{
    [Verb("run")]
    public class RunOptions
    {
        [Option('c', "config", Required = true, HelpText = "Configuration file.")]
        public string ConfigFile { get; set; } = string.Empty;

        [Option('w', "wordlist", Required = true, HelpText = "File contains a list of words.")]
        public string WordlistFile { get; set; } = string.Empty;

        [Option('p', "proxies", HelpText = "File contains a list of proxy.")]
        public string ProxiesFile { get; set; } = string.Empty;

        [Option("proxiesType", HelpText = "Type of proxies.")]
        public string ProxiesType { get; set; } = string.Empty;

        [Option('s', "skip", HelpText = "Number of lines to skip in the wordlist.")]
        public int Skip { get; set; } = -1;

        [Option('b', "bots", Default = 1, HelpText = "Number of bots.")]
        public int Bots { get; set; }

        [Option('v', "verbose", Default = false, HelpText = "Prints task errors.")]
        public bool Verbose { get; set; }
    }

    public class Program
    {
        private const string SettingsFile = "Settings.json";
        private const string ConfigsFolder = "configs";
        private const string WordlistsFolder = "wordlists";

        public static async Task Main(string[] args)
        {
            AnsiConsole.Write(new FigletText("QuickBullet").LeftAligned());

            await GenerateSettingsFile();

            GenerateConfigsFolder();

            GenerateWordlistsFolder();

            Microsoft.Playwright.Program.Main(new string[] { "install" });

            if (args.Any())
            {
                await Parser.Default.ParseArguments<RunOptions>(args).WithParsedAsync(RunAsync);
            }
            else
            {
                var configFiles = GetAllConfigFiles();

                var wordlistsFiles = GetAllWordlistsFiles();

                var config = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Select config:")
                    .AddChoices(configFiles.Select(c => Path.GetFileNameWithoutExtension(c))));

                var wordlists = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Select wordlists:")
                    .AddChoices(wordlistsFiles.Select(c => Path.GetFileNameWithoutExtension(c))));

                var runOptions = new RunOptions
                {
                    ConfigFile = Path.Combine(ConfigsFolder, $"{config}.loli"),
                    WordlistFile = Path.Combine(WordlistsFolder, $"{wordlists}.txt")
                };
                
                runOptions.ProxiesFile = AnsiConsole.Ask("proxies file", "none");

                runOptions.ProxiesType = runOptions.ProxiesFile.Equals("none") ? string.Empty : AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("proxies type")
                    .AddChoices(new string[] { "http", "socks4", "socks5" }));

                runOptions.Skip = AnsiConsole.Ask("skip", -1);

                var bots = AnsiConsole.Ask("bots", 1);

                while (bots > 200)
                {
                    AnsiConsole.MarkupLine("[red]The number of bots must be less than 200[/]");

                    bots = AnsiConsole.Ask("bots", 1);
                }

                runOptions.Bots = bots;

                runOptions.Verbose = AnsiConsole.Ask("verbose:", false);

                await RunAsync(runOptions);
            }
        }

        private static async Task GenerateSettingsFile()
        {
            if (File.Exists(SettingsFile))
            {
                return;
            }

            var quickBulletSettings = new QuickBulletSettings();

            using var streamWriter = new StreamWriter(SettingsFile);

            await streamWriter.WriteAsync(JsonConvert.SerializeObject(quickBulletSettings, Formatting.Indented, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }));
        }

        private static void GenerateConfigsFolder()
        {
            if (File.Exists(ConfigsFolder))
            {
                return;
            }

            Directory.CreateDirectory(ConfigsFolder);
        }

        private static void GenerateWordlistsFolder()
        {
            if (File.Exists(WordlistsFolder))
            {
                return;
            }

            Directory.CreateDirectory(WordlistsFolder);
        }

        private static IEnumerable<string> GetAllConfigFiles()
        {
            var configFiles = Directory.EnumerateFiles(Path.Combine(ConfigsFolder), "*.loli");

            if (configFiles.Any())
            {
                return configFiles;
            }

            AnsiConsole.MarkupLine("[red]The Configs folder does not contain any configs[/]");
            
            Console.ReadKey();
            
            Environment.Exit(0);

            return null;
        }

        private static IEnumerable<string> GetAllWordlistsFiles()
        {
            var wordlistsFiles = Directory.EnumerateFiles(Path.Combine(WordlistsFolder), "*.txt");

            if (wordlistsFiles.Any())
            {
                return wordlistsFiles;
            }

            AnsiConsole.MarkupLine("[red]The Wordlists folder does not contain any wordlists[/]");
            
            Console.ReadKey();
            
            Environment.Exit(0);

            return null;
        }

        private static async Task RunAsync(RunOptions runOptions)
        {
            var proxies = runOptions.ProxiesFile.Equals("none") ? Array.Empty<string>() : File.ReadAllLines(runOptions.ProxiesFile).Where(p => !string.IsNullOrEmpty(p));

            var proxyType = Enum.TryParse<ProxyType>(runOptions.ProxiesType, true, out var result) ? result : ProxyType.Http;

            var checkerBuilder = new CheckerBuilder(runOptions.ConfigFile, runOptions.WordlistFile, proxies, proxyType, runOptions.Skip, runOptions.Bots, runOptions.Verbose);

            var checker = await checkerBuilder.BuildAsync();

            var consoleManager = new ConsoleManager(checker);

            _ = consoleManager.StartUpdatingTitleAsync();

            _ = consoleManager.StartListeningKeysAsync();

            await checker.StartAsync();
        }
    }
}