using QuickBullet.Models;
using System.Text.RegularExpressions;

namespace QuickBullet.Blocks
{
    public abstract class Block
    {
        private readonly Regex _regex;
        private readonly Dictionary<string, Func<string, Match, BotData, string>> _replaceFunctions;

        public Block()
        {
            _regex = new Regex("<([^ ].+?)(?:\\[([^ ].+?)\\])?>", RegexOptions.Compiled);
            _replaceFunctions = new Dictionary<string, Func<string, Match, BotData, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "data.headers", ReplaceWithResponseHeaders },
                { "data.cookies", ReplaceWithResponseCookies }
            };
        }

        public abstract Task RunAsync(BotData botData);

        protected string ReplaceValues(string input, BotData botData)
        {
            foreach (Match match in _regex.Matches(input))
            {
                input = _replaceFunctions.ContainsKey(match.Groups[1].Value) ? _replaceFunctions[match.Groups[1].Value].Invoke(input, match, botData) : ReplaceWithVariableValue(input, match, botData);
            }

            return input;
        }

        private string ReplaceWithResponseHeaders(string input, Match match, BotData botData)
        {
            if (match.Groups[2].Success)
            {
                var header = botData.Headers.SingleOrDefault(h => h.Key.Equals(match.Groups[2].Value, StringComparison.OrdinalIgnoreCase));
                return input.Replace(match.Value, header.Key is null ? string.Empty : header.Value);
            }

            return input.Replace(match.Value, string.Join(Environment.NewLine, botData.Headers.Select(h => $"{h.Key}: {h.Value}")));
        }

        private string ReplaceWithResponseCookies(string input, Match match, BotData botData)
        {
            if (match.Groups[2].Success)
            {
                var cookie = botData.CookieContainer.GetAllCookies().SingleOrDefault(c => c.Name.Equals(match.Groups[2].Value, StringComparison.OrdinalIgnoreCase));
                return input.Replace(match.Value, cookie is null ? string.Empty : cookie.Value);
            }

            return input.Replace(match.Value, string.Join(Environment.NewLine, botData.CookieContainer.GetAllCookies().Select(c => $"{c.Name}={c.Value}")));
        }

        private static string ReplaceWithVariableValue(string input, Match match, BotData botData) => input.Replace(match.Value, botData.Variables.TryGetValue(match.Groups[1].Value, out var value) ? value : string.Empty);
    }
}