namespace QuickBullet.Models
{
    public class BrowserSettings
    {
        public string BrowserType { get; set; } = "chromium";
        public string UrlToAbort { get; set; } = string.Empty;
        public string ResourceTypeToAbort { get; set; } = string.Empty;
        public bool Headless { get; set; } = true;
        public int Height { get; set; } = 720;
        public int Width { get; set; } = 1280;
        public int SlowMo { get; set; } = 0;
        public int Timeout { get; set; } = 30000;
    }
}
