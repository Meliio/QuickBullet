using QuickBullet.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace QuickBullet.Blocks
{
    public class BlockFunction : Block
    {
        public string Action { get; set; } = string.Empty;
        public string HashType { get; set; } = string.Empty;
        public string UserAgentType { get; set; } = string.Empty;
        public string Input { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public bool IsCapture { get; set; } = false;

        private readonly string _lowercase;
        private readonly string _uppercase;
        private readonly string _digits;
        private readonly string _symbols;
        private readonly string _hex;
        private readonly string _udChars;
        private readonly string _ldChars;
        private readonly string _upperlwr;
        private readonly string _ludChars;
        private readonly string _allChars;
        private readonly Dictionary<string, Func<BotData, string>> _functions;
        private readonly Dictionary<string, Func<string, string>> _hashFunctions;
        private readonly Dictionary<string, string[]> _userAgnets;
        private readonly Random _random;

        public BlockFunction()
        {
            _lowercase = "abcdefghijklmnopqrstuvwxyz";
            _uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            _digits = "0123456789";
            _symbols = "\\!\"£$%&/()=?^'{}[]@#,;.:-_*+";
            _hex = _digits + "abcdef";
            _udChars = _uppercase + _digits;
            _ldChars = _lowercase + _digits;
            _upperlwr = _lowercase + _uppercase;
            _ludChars = _lowercase + _uppercase + _digits;
            _allChars = _lowercase + _uppercase + _digits + _symbols;
            _functions = new Dictionary<string, Func<BotData, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "base64Decode", Base64Decode },
                { "base64Encode", Base64Encode },
                { "clearCookies", ClearCookies },
                { "constant", Constant },
                { "currentUnixTime", GetUnixTime },
                { "getRandomUA", GetRandomUserAgent },
                { "hash", Hash },
                { "htmlDecode", HtmlDecode },
                { "htmlEncode", HtmlEncode },
                { "urlDecode", UrlDecode },
                { "urlEncode", UrlEncode },
                { "length", Lenght },
                { "randomString", RandomString },
                { "toLowercase", ToLowercase },
                { "toUppercase", ToUppercase }
            };
            _hashFunctions = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "MD5", HashMD5 },
                { "SHA1", HashSHA1 },
                { "SHA256", HashSHA256 },
                { "SHA384", HashSHA384 },
                { "SHA512", HashSHA512 }
            };

            _userAgnets = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            var userAgentFiles = Directory.EnumerateFiles(Path.Combine("user-agents"), "*.txt");

            foreach (var userAgentFile in userAgentFiles)
            {
                _userAgnets.Add(Path.GetFileNameWithoutExtension(userAgentFile), File.ReadAllLines(userAgentFile));
            }

            _random = new Random();
        }

        public override async Task RunAsync(BotData botData)
        {
            if (Action.Equals("delay", StringComparison.OrdinalIgnoreCase))
            {
                await Task.Delay(int.Parse(Input));
            }
            else
            {
                var result = _functions[Action].Invoke(botData);

                if (string.IsNullOrEmpty(result))
                {
                    return;
                }

                if (IsCapture)
                {
                    botData.Captures[Output] = result;
                }

                botData.Variables[Output] = result;

                return;
            }
        }

        private string Base64Decode(BotData botData) => Encoding.UTF8.GetString(Convert.FromBase64String(ReplaceValues(Input, botData)));

        private string Base64Encode(BotData botData) => Convert.ToBase64String(Encoding.UTF8.GetBytes(ReplaceValues(Input, botData)));

        private string ClearCookies(BotData botData)
        {
            botData.CookieContainer = new CookieContainer();
            return string.Empty;
        }

        private string Constant(BotData botData) => ReplaceValues(Input, botData);

        private string GetUnixTime(BotData botData) => DateTimeOffset.Now.ToUnixTimeSeconds().ToString();

        private string GetRandomUserAgent(BotData botData)
        {
            var userAgents = _userAgnets[UserAgentType];

            return userAgents[_random.Next(userAgents.Length)];
        }

        private string Hash(BotData botData) => _hashFunctions[HashType].Invoke(ReplaceValues(Input, botData));

        private string HashMD5(string input)
        {
            using var md5Hash = MD5.Create();
            return BitConverter.ToString(md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", string.Empty);
        }
        private string HashSHA1(string input)
        {
            using var sha1Hash = SHA1.Create();
            return BitConverter.ToString(sha1Hash.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", string.Empty).ToLower();
        }

        private string HashSHA256(string input)
        {
            using var sha256Hash = SHA256.Create();
            return BitConverter.ToString(sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", string.Empty).ToLower();
        }

        private string HashSHA384(string input)
        {
            using var sha384Hash = SHA384.Create();
            return BitConverter.ToString(sha384Hash.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", string.Empty).ToLower();
        }

        private string HashSHA512(string input)
        {
            using var sha512Hash = SHA512.Create();
            return BitConverter.ToString(sha512Hash.ComputeHash(Encoding.UTF8.GetBytes(input))).Replace("-", string.Empty).ToLower();
        }

        private string HtmlDecode(BotData botData) => HttpUtility.HtmlDecode(ReplaceValues(Input, botData));

        private string HtmlEncode(BotData botData) => HttpUtility.HtmlEncode(ReplaceValues(Input, botData));

        private string UrlDecode(BotData botData) => HttpUtility.UrlDecode(ReplaceValues(Input, botData));

        private string UrlEncode(BotData botData) => HttpUtility.UrlEncode(ReplaceValues(Input, botData));

        private string Lenght(BotData botData) => ReplaceValues(Input, botData).Length.ToString();

        private string RandomString(BotData botData)
        {
            var result = ReplaceValues(Input, botData);

            result = Regex.Replace(result, @"\?l", m => _lowercase[_random.Next(26)].ToString());
            result = Regex.Replace(result, @"\?u", m => _uppercase[_random.Next(26)].ToString());
            result = Regex.Replace(result, @"\?d", m => _digits[_random.Next(10)].ToString());
            result = Regex.Replace(result, @"\?s", m => _symbols[_random.Next(28)].ToString());
            result = Regex.Replace(result, @"\?h", m => _hex[_random.Next(16)].ToString());
            result = Regex.Replace(result, @"\?a", m => _allChars[_random.Next(90)].ToString());
            result = Regex.Replace(result, @"\?m", m => _udChars[_random.Next(36)].ToString());
            result = Regex.Replace(result, @"\?n", m => _ldChars[_random.Next(36)].ToString());
            result = Regex.Replace(result, @"\?i", m => _ludChars[_random.Next(62)].ToString());
            result = Regex.Replace(result, @"\?f", m => _upperlwr[_random.Next(52)].ToString());

            return result;
        }

        private string ToLowercase(BotData botData) => ReplaceValues(Input, botData).ToLower();

        private string ToUppercase(BotData botData) => ReplaceValues(Input, botData).ToUpper();
    }
}
