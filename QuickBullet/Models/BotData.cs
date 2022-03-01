using Microsoft.Playwright;
using System.Net;

namespace QuickBullet.Models
{
    public class BotData : IAsyncDisposable
    {
        public QuickBulletSettings QuickBulletSettings { get; }
        public BotInput Input { get; }
        public HttpClient HttpClient { get; }
        public HttpClient ProxyHttpClient { get; }
        public IPlaywright Playwright { get; }
        public IEnumerable<KeyValuePair<string, string>> Headers { get; set; }
        public CookieContainer CookieContainer { get; set; }
        public Dictionary<string, string> Variables { get; }
        public Dictionary<string, string> Captures { get; }
        public bool UseProxy { get; set; }

        private readonly Dictionary<string, object> _objects;

        public BotData(QuickBulletSettings quickBulletSettings, BotInput input, HttpClient httpClient, HttpClient proxyHttpClient, IPlaywright playwright)
        {
            QuickBulletSettings = quickBulletSettings;
            Input = input;
            HttpClient = httpClient;
            ProxyHttpClient = proxyHttpClient;
            Playwright = playwright;
            Headers = Array.Empty<KeyValuePair<string, string>>();
            CookieContainer = new CookieContainer();
            Variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "botStatus", "none" },
                { "input", input.ToString() },
                { "input.user", input.Combo.Username },
                { "input.pass", input.Combo.Password },
                { "input.username", input.Combo.Username },
                { "input.password", input.Combo.Password }
            };
            Captures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _objects = new Dictionary<string, object>();
        }

        public void SetObject(string name, object item) => _objects[name] = item;

        public T? TryGetObject<T>(string name) where T : class => _objects.ContainsKey(name) && _objects[name] is T t ? t : null;

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncInternal();
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncInternal()
        {
            var browser = TryGetObject<IBrowser>("playwrightBrowser");

            if (browser is not null)
            {
                await browser.DisposeAsync();
            }

            await Task.CompletedTask;
        }
    }
}