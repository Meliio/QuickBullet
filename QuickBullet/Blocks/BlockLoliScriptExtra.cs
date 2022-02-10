using QuickBullet.Models;

namespace QuickBullet.Blocks
{
    public class BlockLoliScriptExtra : Block
    {
        public string Action { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public bool UseProxy { get; set; }
        private readonly Dictionary<string, Action<BotData>> _setFunctions;

        public BlockLoliScriptExtra()
        {
            _setFunctions = new Dictionary<string, Action<BotData>>(StringComparer.OrdinalIgnoreCase)
            {
                { "setCap", SetCapture },
                { "useProxy", SetUseProxy },
                { "setVar", SetVariable },
            };
        }

        public override Task RunAsync(BotData botData)
        {
            _setFunctions[Action].Invoke(botData);

            return Task.CompletedTask;
        }

        private void SetCapture(BotData botData) => botData.Captures[Name] = ReplaceValues(Value, botData);

        private void SetUseProxy(BotData botData) => botData.UseProxy = UseProxy;

        private void SetVariable(BotData botData) => botData.Variables[Name] = ReplaceValues(Value, botData);
    }
}
