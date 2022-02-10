namespace QuickBullet.Models.Blocks
{
    public class Keycheck
    {
        public List<Keychain> Keychains { get; set; } = new List<Keychain>();
        public bool BanIfNoMatch { get; set; } = true;
    }
}
