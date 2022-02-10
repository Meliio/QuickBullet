namespace QuickBullet.Models.Blocks
{
    public class Extractor
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public string Left { get; set; } = string.Empty;
        public string Right { get; set; } = string.Empty;
        public string Selector { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
        public string Json { get; set; } = string.Empty;
        public string Regex { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool IsCapture { get; set; } = false;
    }
}
