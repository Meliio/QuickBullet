namespace QuickBullet.Models
{
    public class BotInput
    {
        public Combo Combo { get; }

        private readonly string _input;

        public BotInput(string input)
        {
            Combo = new Combo(input);
            _input = input;
        }

        public override string ToString() => _input;
    }
}
