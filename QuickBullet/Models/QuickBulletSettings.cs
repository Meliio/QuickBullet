namespace QuickBullet.Models
{
    public class QuickBulletSettings
    {
        public BrowserSettings Browser { get; set; } = new BrowserSettings();
        public string OutputSeparator { get; set; } = " | ";
        public string OutputDirectory { get; set; } = "results";
    }
}
