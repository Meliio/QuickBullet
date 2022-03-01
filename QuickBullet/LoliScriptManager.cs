using Newtonsoft.Json;
using QuickBullet.Blocks;
using QuickBullet.Models;
using QuickBullet.Models.Blocks;
using System.Text;
using System.Text.RegularExpressions;

namespace QuickBullet
{
    public class LoliScriptManager
    {
        private readonly Dictionary<string, Func<string, Block>> _buildBlockFunctions;

        public LoliScriptManager()
        {
            _buildBlockFunctions = new Dictionary<string, Func<string, Block>>(StringComparer.OrdinalIgnoreCase)
            {
                { "browserAction", BuildBlockBrowserAction },
                { "parse", BuildBlockExtractor },
                { "function", BuildBlockFunction },
                { "keycheck", BuildBlockKeycheck },
                { "pageAction", BuildBlockPageAction },
                { "request", BuildBlockRequest }
            };
        }

        public (ConfigSettings, IEnumerable<Block>) Build(string configPath)
        {
            var lines = File.ReadAllLines(configPath).Where(l => !string.IsNullOrEmpty(l));

            var settings = new StringBuilder();

            var script = new List<string>();

            var isScript = !lines.Any(l => l.Trim().Equals("[SCRIPT]"));

            foreach (var line in lines.First().Equals("[SETTINGS]", StringComparison.OrdinalIgnoreCase) ? lines.Skip(1) : lines)
            {
                if (line.Equals("[SCRIPT]"))
                {
                    isScript = true;
                }
                else
                {
                    var lineTrimed = line.Trim();

                    if (isScript)
                    {
                        if (line.StartsWith('!'))
                        {
                            continue;
                        }
                        else if (line.StartsWith(' '))
                        {
                            script[^1] += $" {lineTrimed}";
                        }
                        else
                        {
                            script.Add(lineTrimed);
                        }
                    }
                    else
                    {
                        settings.Append(lineTrimed);
                    }
                }
            }

            var configSettings = string.IsNullOrEmpty(settings.ToString()) ? new ConfigSettings() : JsonConvert.DeserializeObject<ConfigSettings>(settings.ToString());

            var blocks = new List<Block>();

            foreach (var line in script)
            {
                var match = Regex.Match(line, "^(#[^ ]* )?([^ ]*)");

                var blockName = match.Groups[2].Value;

                if (_buildBlockFunctions.ContainsKey(blockName))
                {
                    blocks.Add(_buildBlockFunctions[blockName].Invoke(line[match.Value.Length..].TrimStart()));
                }
                else
                {
                    if (blockName.Equals("SET", StringComparison.OrdinalIgnoreCase))
                    {
                        blocks.Add(BuildBlockLoliScriptExtra(line[match.Length..].TrimStart()));
                    }
                }
            }       
                      
            return (configSettings, blocks);
        }

        private BlockBrowserAction BuildBlockBrowserAction(string script)
        {
            var blockBrowserAction = new BlockBrowserAction();

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "CLEARCOOKIES":
                        blockBrowserAction.Action = token;
                        break;
                    case "CLOSE":
                        blockBrowserAction.Action = token;
                        break;
                    case "GETCOOKIES":
                        blockBrowserAction.Action = token;
                        break;
                    case "OPEN":
                        blockBrowserAction.Action = token;
                        break;
                    case "SETCOOKIES":
                        blockBrowserAction.Action = token;
                        break;
                }
            }

            return blockBrowserAction;
        }

        private BlockExtractor BuildBlockExtractor(string script)
        {
            var blockExtractor = new BlockExtractor
            {
                Source = GetToken(ref script, true)
            };

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "LR":
                        blockExtractor.Type = token;
                        blockExtractor.Left = GetToken(ref script, true);
                        blockExtractor.Right = GetToken(ref script, true);
                        break;
                    case "CSS":
                        blockExtractor.Type = token;
                        blockExtractor.Selector = GetToken(ref script, true);
                        blockExtractor.Attribute = GetToken(ref script, true);
                        break;
                    case "XPATH":
                        blockExtractor.Type = token;
                        blockExtractor.Selector = GetToken(ref script, true);
                        blockExtractor.Attribute = GetToken(ref script, true);
                        break;
                    case "JSON":
                        blockExtractor.Type = token;
                        blockExtractor.Json = GetToken(ref script, true);
                        break;
                    case "REGEX":
                        blockExtractor.Type = token;
                        blockExtractor.Regex = GetToken(ref script, true);
                        blockExtractor.Group = GetToken(ref script, true);
                        break;
                    default:
                        var tokenSplit = token.Split('=');
                        if (tokenSplit.Length == 2)
                        {
                            switch (tokenSplit[0].ToUpper())
                            {
                                case "JTOKENPARSING":
                                    blockExtractor.UseJToken = bool.Parse(tokenSplit[1]);
                                    break;
                            }
                        }
                        else if (token.Equals("->"))
                        {
                            switch (GetToken(ref script).ToUpper())
                            {
                                case "VAR":
                                    blockExtractor.Name = GetToken(ref script, true);
                                    break;
                                case "CAP":
                                    blockExtractor.Name = GetToken(ref script, true);
                                    blockExtractor.IsCapture = true;
                                    break;
                            }
                        }
                        break;
                }
            }

            return blockExtractor;
        }

        private BlockFunction BuildBlockFunction(string script)
        {
            var blockFunction = new BlockFunction();

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "BASE64DECODE":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "BASE64ENCODE":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "CLEARCOOKIES":
                        blockFunction.Action = token;
                        break;
                    case "DELAY":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "CONSTANT":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "CURRENTUNIXTIME":
                        blockFunction.Action = token;
                        break;
                    case "GETRANDOMUA":
                        blockFunction.Action = token;
                        blockFunction.UserAgentType = GetToken(ref script, true);
                        break;
                    case "HASH":
                        blockFunction.Action = token;
                        blockFunction.HashType = GetToken(ref script);
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "HTMLDECODE":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "HTMLENCODE":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "URLDECODE":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "URLENCODE":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "LENGTH":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "RANDOMSTRING":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "TOLOWERCASE":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    case "TOUPPERCASE":
                        blockFunction.Action = token;
                        blockFunction.Input = GetToken(ref script, true);
                        break;
                    default:
                        if (token.Equals("->"))
                        {
                            switch (GetToken(ref script).ToUpper())
                            {
                                case "VAR":
                                    blockFunction.Output = GetToken(ref script, true);
                                    break;
                                case "CAP":
                                    blockFunction.Output = GetToken(ref script, true);
                                    blockFunction.IsCapture = true;
                                    break;
                            }
                        }
                        break;
                }
            }

            return blockFunction;
        }

        private BlockKeycheck BuildBlockKeycheck(string script)
        {
            var keycheck = new Keycheck();

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "KEYCHAIN":
                        var status = GetToken(ref script);
                        keycheck.Keychains.Add(new Keychain
                        {
                            Status = status.ToUpper().Equals("CUSTOM") ? GetToken(ref script, true) : status,
                            Condition = GetToken(ref script)
                        });
                        break;
                    case "KEY":
                        var item = GetToken(ref script, true);
                        if (script.StartsWith("lessThan", StringComparison.OrdinalIgnoreCase) || script.StartsWith("greaterThan", StringComparison.OrdinalIgnoreCase) || script.StartsWith("equalTo", StringComparison.OrdinalIgnoreCase) || script.StartsWith("notEqualTo", StringComparison.OrdinalIgnoreCase) || script.StartsWith("contains", StringComparison.OrdinalIgnoreCase) || script.StartsWith("doesNotContain", StringComparison.OrdinalIgnoreCase) || script.StartsWith("matchesRegex", StringComparison.OrdinalIgnoreCase) || script.StartsWith("doesNotMatchRegex", StringComparison.OrdinalIgnoreCase))
                        {
                            keycheck.Keychains[^1].Keys.Add(new Key
                            {
                                Source = item,
                                Condition = GetToken(ref script),
                                Value = GetToken(ref script, true)
                            });
                        }
                        else
                        {
                            keycheck.Keychains[^1].Keys.Add(new Key
                            {
                                Value = item
                            });
                        }
                        break;
                    default:
                        var tokenSplit = token.Split('=');
                        if (tokenSplit.Length == 2)
                        {
                            switch (tokenSplit[0].ToUpper())
                            {
                                case "BANONTOCHECK":
                                    keycheck.BanIfNoMatch = bool.Parse(tokenSplit[1]);
                                    break;
                            }
                        }
                        break;
                }
            }

            return new BlockKeycheck(keycheck);
        }

        private BlockLoliScriptExtra BuildBlockLoliScriptExtra(string script)
        {
            var blockLoliScriptExtra = new BlockLoliScriptExtra();

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "CAP":
                        blockLoliScriptExtra.Action = token;
                        blockLoliScriptExtra.Name = GetToken(ref script, true);
                        blockLoliScriptExtra.Value = GetToken(ref script, true);
                        break;
                    case "USEPROXY":
                        blockLoliScriptExtra.Action = token;
                        blockLoliScriptExtra.UseProxy = bool.Parse(GetToken(ref script, true));
                        break;
                    case "VAR":
                        blockLoliScriptExtra.Action = token;
                        blockLoliScriptExtra.Name = GetToken(ref script, true);
                        blockLoliScriptExtra.Value = GetToken(ref script, true);
                        break;
                }
            }

            return blockLoliScriptExtra;
        }

        private BlockPageAction BuildBlockPageAction(string script)
        {
            var blockPageAction = new BlockPageAction();

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "CLICK":
                        blockPageAction.Action = token;
                        blockPageAction.Selector = GetToken(ref script, true);
                        break;
                    case "DELAY":
                        blockPageAction.Delay = int.Parse(GetToken(ref script, true));
                        break;
                    case "EVALUATE":
                        blockPageAction.Action = token;
                        blockPageAction.Expression = GetToken(ref script, true);
                        break;
                    case "GOTO":
                        blockPageAction.Action = token;
                        blockPageAction.Url = GetToken(ref script, true);
                        break;
                    case "GETATTRIBUTE":
                        blockPageAction.Action = token;
                        blockPageAction.Selector = GetToken(ref script, true);
                        blockPageAction.Attribute = GetToken(ref script, true);
                        break;
                    case "GETADDRESS":
                        blockPageAction.Action = token;
                        break;
                    case "GETCONTENT":
                        blockPageAction.Action = token;
                        break;
                    case "HEADER":
                        blockPageAction.Headers.Add(GetToken(ref script, true));
                        break;
                    case "PRESSKEY":
                        blockPageAction.Action = token;
                        blockPageAction.Key = GetToken(ref script, true);
                        break;
                    case "RELOAD":
                        blockPageAction.Action = token;
                        break;
                    case "SENDKEY":
                        blockPageAction.Action = token;
                        blockPageAction.Selector = GetToken(ref script, true);
                        blockPageAction.Text = GetToken(ref script, true);
                        break;
                    case "SETHEADERS":
                        blockPageAction.Action = token;
                        break;
                    case "WAITFORRESPONSE":
                        blockPageAction.Action = token;
                        blockPageAction.Url = GetToken(ref script, true);
                        break;
                    case "WAITFORSELECTOR":
                        blockPageAction.Action = token;
                        blockPageAction.Selector = GetToken(ref script, true);
                        break;
                    case "WAITFORTIMEOUT":
                        blockPageAction.Action = token;
                        blockPageAction.Timeout = int.Parse(GetToken(ref script, true));
                        break;
                    default:
                        if (token.Equals("->"))
                        {
                            switch (GetToken(ref script).ToUpper())
                            {
                                case "VAR":
                                    blockPageAction.Output = GetToken(ref script, true);
                                    break;
                                case "CAP":
                                    blockPageAction.Output = GetToken(ref script, true);
                                    blockPageAction.IsCapture = true;
                                    break;
                            }
                        }
                        break;
                }
            }

            return blockPageAction;
        }

        private BlockRequest BuildBlockRequest(string script)
        {
            var httpMethod = new HttpMethod(GetToken(ref script));

            var url = GetToken(ref script, true);

            var encodeContent = false;

            var loadContent = true;

            var type = "standard";

            var contents = new List<string>();

            var contentType = "application/x-www-form-urlencoded";

            var headers = new Dictionary<string, string>();

            var cookies = new List<string>();

            while (!string.IsNullOrEmpty(script))
            {
                var token = GetToken(ref script);

                switch (token.ToUpper())
                {
                    case "HEADER":
                        var headerSplit = GetToken(ref script, true).Split(':', 2, StringSplitOptions.TrimEntries);
                        if (headerSplit.Length == 2)
                        {
                            if (headerSplit[0].Equals("cookie", StringComparison.OrdinalIgnoreCase))
                            {
                                cookies.AddRange(headerSplit[1].Split(';', StringSplitOptions.TrimEntries));
                            }
                            else if (headerSplit[0].Equals("accept-encoding", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            else
                            {
                                headers.Add(headerSplit[0], headerSplit[1]);
                            }
                        }
                        break;
                    case "COOKIE":
                        var cookieSplit = GetToken(ref script, true).Split(':', 2, StringSplitOptions.TrimEntries);
                        if (cookieSplit.Length == 2)
                        {
                            cookies.Add(string.Join('=', cookieSplit));
                        }
                        break;
                    case "CONTENT":
                        contents.Add(GetToken(ref script, true));
                        break;
                    case "STRINGCONTENT":
                        contents.Add(GetToken(ref script, true));
                        break;
                    case "CONTENTTYPE":
                        contentType = GetToken(ref script, true).Split(';')[0];
                        break;
                    case "MULTIPART":
                        type = token;
                        break;
                    default:
                        var tokenSplit = token.Split('=');
                        if (tokenSplit.Length == 2)
                        {
                            if (bool.TryParse(tokenSplit[1], out var result))
                            {
                                switch (tokenSplit[0].ToUpper())
                                {
                                    case "READRESPONSESOURCE":
                                        loadContent = result;
                                        break;
                                }
                            }
                        }
                        break;
                }
            }

            var request = new Request(httpMethod, url, headers, string.Join(", ", cookies), type, contents, contentType, loadContent);

            return new BlockRequest(request);
        }

        private string GetToken(ref string input, bool isLiteral = false)
        {
            var match = isLiteral ? Regex.Match(input, "\"(\\\\.|[^\\\"])*\"") : Regex.Match(input, "^[^ ]*");

            input = input[match.Value.Length..].TrimStart();

            return isLiteral ? match.Value[1..^1].Replace("\\\"", "\"") : match.Value;      
        }
    }
}
