namespace QuickBullet.Models
{
    public class Combo
    {
        public string Username { get; } = string.Empty;
        public string Password { get; } = string.Empty;

        public Combo(string combo)
        {
            var comboSplit = combo.Split(':', 2, StringSplitOptions.RemoveEmptyEntries);

            if (comboSplit.Length == 2)
            {
                Username = comboSplit[0];
                Password = comboSplit[1];
            }
        }
    }
}
