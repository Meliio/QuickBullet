using Microsoft.Playwright;
using QuickBullet.Models;
using System.Text.RegularExpressions;

namespace QuickBullet.Blocks
{
    public class BlockBrowserAction : Block
    {
        public string Action { get; set; } = string.Empty;

        private readonly Dictionary<string, Func<BotData, Task>> _browserFunction;

        public BlockBrowserAction()
        {
            _browserFunction = new Dictionary<string, Func<BotData, Task>>(StringComparer.OrdinalIgnoreCase)
            {
                { "clearCookies", ClearCookiesBrowserAsync },
                { "close", CloseBrowserAsync },
                { "getCookies", GetCookiesBrowserAsync },
                { "open", OpenBrowserAsync },
                { "setCookies", SetCookiesBrowserAsync }
            };
        }

        public override async Task RunAsync(BotData botData) => await _browserFunction[Action].Invoke(botData);

        private static async Task<IBrowser> BuildBrowserAsync(BotData botData)
        {
            var launchOptions = new BrowserTypeLaunchOptions()
            {
                Headless = botData.QuickBulletSettings.Browser.Headless,
                Proxy = botData.UseProxy ? BuilPlaywrightdProxy(botData.TryGetObject<Models.Proxy>("proxy")) : null,
                SlowMo = botData.QuickBulletSettings.Browser.SlowMo,
                Timeout = botData.QuickBulletSettings.Browser.Timeout
            };

            return botData.QuickBulletSettings.Browser.BrowserType switch
            {
                string value when value.Equals("chromium", StringComparison.OrdinalIgnoreCase) => await botData.Playwright.Chromium.LaunchAsync(launchOptions),
                string value when value.Equals("firefox", StringComparison.OrdinalIgnoreCase) => await botData.Playwright.Firefox.LaunchAsync(launchOptions),
                string value when value.Equals("webkit", StringComparison.OrdinalIgnoreCase) => await botData.Playwright.Webkit.LaunchAsync(launchOptions),
                _ => await botData.Playwright.Chromium.LaunchAsync(launchOptions)
            };
        }

        private static Microsoft.Playwright.Proxy? BuilPlaywrightdProxy(Models.Proxy? proxy)
        {
            if (proxy is null)
            {
                return null;
            }

            return new Microsoft.Playwright.Proxy()
            {
                Server = $"{(proxy.Type.Equals("http", StringComparison.OrdinalIgnoreCase) ? "http" : "socks")}://{proxy.Host}:{proxy.Port}",
                Username = proxy.Username,
                Password = proxy.Password
            };
        }

        private static async Task ClearCookiesBrowserAsync(BotData botData) => await botData.TryGetObject<IBrowser>("playwrightBrowser").Contexts[0].ClearCookiesAsync();

        private static async Task CloseBrowserAsync(BotData botData) => await botData.TryGetObject<IBrowser>("playwrightBrowser").DisposeAsync();

        private static async Task GetCookiesBrowserAsync(BotData botData)
        {
            foreach (var cookie in await botData.TryGetObject<IBrowser>("playwrightBrowser").Contexts[0].CookiesAsync())
            {
                botData.CookieContainer.Add(new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain));
            }
        }

        private static async Task OpenBrowserAsync(BotData botData)
        {
            var browser = await BuildBrowserAsync(botData);

            var page = await browser.NewPageAsync(new BrowserNewPageOptions()
            { 
                ViewportSize = new ViewportSize() 
                { 
                    Height = botData.QuickBulletSettings.Browser.Height, Width = botData.QuickBulletSettings.Browser.Width 
                } 
            });

            var regexUrl = new Regex(string.IsNullOrEmpty(botData.QuickBulletSettings.Browser.UrlToAbort) ? "^$" : botData.QuickBulletSettings.Browser.UrlToAbort);

            var regexResourceType = new Regex(string.IsNullOrEmpty(botData.QuickBulletSettings.Browser.ResourceTypeToAbort) ? "^$" : botData.QuickBulletSettings.Browser.ResourceTypeToAbort);

            await page.RouteAsync("**/*", route =>
            {
                if (regexUrl.IsMatch(route.Request.Url) || regexResourceType.IsMatch(route.Request.ResourceType))
                {
                    route.AbortAsync();
                }
                else
                {
                    route.ContinueAsync();
                }
            });

            botData.SetObject("playwrightBrowser", browser);

            botData.SetObject("playwrightPage", page);
        }

        private static async Task SetCookiesBrowserAsync(BotData botData) => await botData.TryGetObject<IBrowser>("playwrightBrowser").Contexts[0].AddCookiesAsync(botData.CookieContainer.GetAllCookies().Select(c => new Cookie() { Name = c.Name, Value = c.Value, Path = c.Path, Domain = c.Domain }));
    }
}
