using Microsoft.Playwright;
using QuickBullet.Models;

namespace QuickBullet.Blocks
{
    public class BlockPageAction : Block
    {
        public string Action { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string Selector { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int Delay { get; set; }
        public int Timeout { get; set; }
        public bool IsCapture { get; set; }
        public List<string> Headers { get; set; } = new List<string>();

        private readonly Dictionary<string, Func<IPage, BotData, Task>> _pageFunction;
        private readonly Dictionary<string, Func<IPage, Task<string>>> _getSelectorAttributeFunctions;

        public BlockPageAction()
        {
            _pageFunction = new Dictionary<string, Func<IPage, BotData, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                { "click", ClickPageAsync },
                { "evaluate", EvaluatePageAsync },
                { "getAttribute", GetAttributePageAsync },
                { "getAddress", GetAddressPageAsync },
                { "getContent", GetContentPageAsync },
                { "goTo", GotoPageAsync },
                { "pressKey", PressKeyPageAsync },
                { "reload", ReloadPageAsync },
                { "sendKey", TypePageAsync },
                { "setHeaders", SetHeadersBrowserAsync },
                { "waitForResponse", WaitForResponsePageAsync },
                { "waitForSelector", WaitForSelectorPageAsync },
                { "waitForTimeout", WaitForTimeoutPageAsync }
            };
            _getSelectorAttributeFunctions = new Dictionary<string, Func<IPage, Task<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                { "innerHTML", GetAttributeInnerHTMLAsync },
                { "innerText", GetAttributeInnerTextAsync },
                { "textContent", GetAttributeTextContentAsync }
            };
        }

        public override async Task RunAsync(BotData botData)
        {
            var page = botData.TryGetObject<IPage>("playwrightPage");

            await _pageFunction[Action].Invoke(page, botData);
        }

        public async Task ClickPageAsync(IPage page, BotData botData) => await page.ClickAsync(Selector);

        public async Task EvaluatePageAsync(IPage page, BotData botData)
        {
            var result = await page.EvaluateAsync<string>(Expression);

            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            if (IsCapture)
            {
                botData.Captures[Output] = result;
            }

            botData.Variables[Output] = result;
        }

        public async Task GetAttributePageAsync(IPage page, BotData botData)
        {
            var result = _getSelectorAttributeFunctions.ContainsKey(Selector) ? await _getSelectorAttributeFunctions[Selector].Invoke(page) : await page.GetAttributeAsync(Selector, Attribute);

            if (string.IsNullOrEmpty(result))
            {
                return;
            }

            if (IsCapture)
            {
                botData.Captures[Output] = result;
            }

            botData.Variables[Output] = result;
        }

        public static Task GetAddressPageAsync(IPage page, BotData botData)
        {
            botData.Variables["data.address"] = page.Url;
            return Task.CompletedTask;
        }

        public static async Task GetContentPageAsync(IPage page, BotData botData) => botData.Variables["data.source"] = await page.ContentAsync();

        public async Task GotoPageAsync(IPage page, BotData botData) => await page.GotoAsync(Url);

        public async Task PressKeyPageAsync(IPage page, BotData botData) => await page.Keyboard.PressAsync(Key);

        public static async Task ReloadPageAsync(IPage page, BotData botData) => await page.ReloadAsync();

        public async Task TypePageAsync(IPage page, BotData botData) => await page.TypeAsync(Selector, ReplaceValues(Text, botData), new PageTypeOptions() { Delay = Delay});

        public async Task WaitForResponsePageAsync(IPage page, BotData botData)
        {
            var response = await page.WaitForResponseAsync(Url);
            botData.Variables["data.address"] = response.Url;
            botData.Variables["data.statusCode"] = response.Status.ToString();
            botData.Headers = await response.AllHeadersAsync();
            botData.Variables["data.source"] = await response.TextAsync();
        }

        private async Task SetHeadersBrowserAsync(IPage page, BotData botData)
        {
            var headers = new List<KeyValuePair<string, string>>();

            foreach (var header in Headers)
            {
                var headerSplit = header.Split(':', 2, StringSplitOptions.TrimEntries);

                if (headerSplit.Length == 2)
                {
                    headers.Add(new KeyValuePair<string, string>(headerSplit[0], ReplaceValues(headerSplit[1], botData)));
                }
            }

            await page.SetExtraHTTPHeadersAsync(headers);
        }

        public async Task WaitForSelectorPageAsync(IPage page, BotData botData) => await page.WaitForSelectorAsync(Selector);

        public async Task WaitForTimeoutPageAsync(IPage page, BotData botData) => await page.WaitForTimeoutAsync(Timeout);

        private async Task<string> GetAttributeInnerHTMLAsync(IPage page) => await page.InnerHTMLAsync(Selector);

        private async Task<string> GetAttributeInnerTextAsync(IPage page) => await page.InnerTextAsync(Selector);

        private async Task<string> GetAttributeTextContentAsync(IPage page) => await page.TextContentAsync(Selector) ?? string.Empty;
    }
}
