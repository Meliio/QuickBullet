using QuickBullet.Models;

namespace QuickBullet
{
    public class ProxyManager
    {
        private readonly List<Proxy> _proxies;
        private readonly Random _random;

        public ProxyManager(IEnumerable<Proxy> proxies)
        {
            _proxies = new List<Proxy>(proxies);
            _random = new Random();
        }

        public async Task StartValidateAllProxiesAsync()
        {
            var periodicTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));

            while (true)
            {
                if (_proxies.Any(p => p.IsValid))
                {
                    await periodicTimer.WaitForNextTickAsync();
                }
                else
                {
                    _proxies.ForEach(p => p.IsValid = true);
                }
            }
        }

        public Proxy GetRandomProxy()
        {
            var proxies = _proxies.Where(p => p.IsValid);

            if (proxies.Any())
            {
                return proxies.ElementAt(_random.Next(proxies.Count()));
            }

            return _proxies[_random.Next(_proxies.Count)];
        }
    }
}
