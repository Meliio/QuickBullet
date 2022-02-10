namespace QuickBullet.Models.Blocks
{
    public class Keychain
    {
        public string Status { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public List<Key> Keys { get; set; } = new List<Key>();
    }
}
