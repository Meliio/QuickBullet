namespace QuickBullet.Models.Blocks
{
    public class Request
    {
        public HttpMethod Method { get; }
        public string Url { get; }
        public Dictionary<string, string> Headers { get; }
        public string CookieHeader { get; }
        public string Type { get; }
        public IEnumerable<string> StringContents { get; }
        public string ContentType { get; }
        public bool LoadContent { get; }

        public Request(HttpMethod method, string url, Dictionary<string, string> headers, string cookieHeader, string type, IEnumerable<string> stringContents, string contentType, bool loadContent)
        {
            Method = method;
            Url = url;
            Headers = headers;
            CookieHeader = cookieHeader;
            Type = type;
            StringContents = stringContents;
            ContentType = contentType;
            LoadContent = loadContent;
        }
    }
}
