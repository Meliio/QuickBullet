namespace QuickBullet.Models
{
    public class ProxyHttpClient : HttpClient
    {
        public Proxy? Proxy { get; }
        public bool IsValid { get; set; }

        public ProxyHttpClient(HttpClientHandler httpClientHandler, Proxy? proxy) : base(httpClientHandler)
        {
            Proxy = proxy;
            IsValid = true;
        }
    }
}
