namespace QuickBullet.Models.Blocks
{
    public class Key
    {
        public string Source { get; set; } = "<data.SOURCE>";
        public string Condition { get; set; } = "contains";
        public string Value { get; set; } = string.Empty;
    }
}
