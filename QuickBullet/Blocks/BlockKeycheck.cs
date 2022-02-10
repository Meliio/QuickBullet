using QuickBullet.Models;
using QuickBullet.Models.Blocks;
using System.Text.RegularExpressions;

namespace QuickBullet.Blocks
{
    public class BlockKeycheck : Block
    {
        private readonly Keycheck _keycheck;
        private readonly Dictionary<string, Func<string, string, bool>> _keyConditionFunctions;
        private readonly Dictionary<string, Func<IEnumerable<bool>, bool>> _keychainConditionFunctions;

        public BlockKeycheck(Keycheck keycheck)
        {
            _keycheck = keycheck;
            _keyConditionFunctions = new Dictionary<string, Func<string, string, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "lessThan", LessThan },
                { "greaterThan", GreaterThan },
                { "equalTo", EqualTo },
                { "notEqualTo", NotEqualTo },
                { "contains", Contains },
                { "doesNotContain", DoesNotContain },
                { "matchesRegex", MatchesRegex },
                { "doesNotMatchRegex", DoesNotMatchRegex }
            };
            _keychainConditionFunctions = new Dictionary<string, Func<IEnumerable<bool>, bool>>(StringComparer.OrdinalIgnoreCase)
            {
                { "or", AnyCondition },
                { "and", AllCondition }
            };
        }

        public override Task RunAsync(BotData botData)
        {
            var success = false;

            foreach (var keychain in _keycheck.Keychains)
            {
                if (_keychainConditionFunctions[keychain.Condition].Invoke(keychain.Keys.Select(k => _keyConditionFunctions[k.Condition].Invoke(ReplaceValues(k.Value, botData), ReplaceValues(k.Source, botData)))))
                {
                    botData.Variables["botStatus"] = keychain.Status;
                    success = true;
                }
            }

            if (success)
            {
                return Task.CompletedTask;
            }

            if (_keycheck.BanIfNoMatch)
            {
                botData.Variables["botStatus"] = "ban";
            }

            return Task.CompletedTask;
        }

        private static bool AnyCondition(IEnumerable<bool> inputs) => inputs.Any(i => i);

        private static bool AllCondition(IEnumerable<bool> inputs) => inputs.All(i => i);

        private static bool LessThan(string value, string part) => int.Parse(part) < int.Parse(value);

        private static bool GreaterThan(string value, string part) => int.Parse(part) > int.Parse(value);

        private static bool EqualTo(string value, string part) => part.Equals(value);

        private static bool NotEqualTo(string value, string part) => !part.Equals(value);

        private static bool Contains(string value, string part) => part.Contains(value);

        private static bool DoesNotContain(string value, string part) => !part.Contains(value);

        private static bool MatchesRegex(string value, string part) => Regex.IsMatch(part, value);

        private static bool DoesNotMatchRegex(string value, string part) => !Regex.IsMatch(part, value);
    }
}
