using QuickBullet.Models;

namespace QuickBullet
{
    public class ProxyHttpClientManager
    {
        private readonly List<ProxyHttpClient> _proxyHttpClients;
        private readonly Random _random;

        public ProxyHttpClientManager(IEnumerable<Proxy> proxies)
        {
            _proxyHttpClients = new List<ProxyHttpClient>();
            if (proxies.Any())
            {
                _proxyHttpClients.AddRange(proxies.Select(p => new ProxyHttpClient(new HttpClientHandler() { UseCookies = false, Proxy = p }, p)));
            }
            else
            {
                _proxyHttpClients.Add(new ProxyHttpClient(new HttpClientHandler() { UseCookies = false }, null));
            }
            _random = new Random();
        }

        public async Task StartValidateAllProxiesAsync()
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            while (true)
            {
                if (_proxyHttpClients.Any(p => p.IsValid))
                {
                    await periodicTimer.WaitForNextTickAsync();
                }
                else
                {
                    _proxyHttpClients.ForEach(p => p.IsValid = true);
                }
            }
        }

        public ProxyHttpClient GetRandomProxyHttpClient()
        {
            var proxies = _proxyHttpClients.Where(p => p.IsValid);

            if (proxies.Any())
            {
                return proxies.ElementAt(_random.Next(proxies.Count()));
            }

            return _proxyHttpClients[_random.Next(_proxyHttpClients.Count)];
        }
    }
}
