using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using QuickBullet.Models;

namespace QuickBullet.Blocks
{
    public class BlockExtractor : Block
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public string Left { get; set; } = string.Empty;
        public string Right { get; set; } = string.Empty;
        public string Selector { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
        public string Json { get; set; } = string.Empty;
        public string Regex { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool IsCapture { get; set; } = false;

        private readonly Dictionary<string, Func<string, string>> _extractorFunctions;
        private readonly Dictionary<string, Func<IElement, string>> _getCssAttributeFunctions;
        private readonly Dictionary<string, Func<HtmlNode, string>> _getXPathAttributeFunctions;

        public BlockExtractor()
        {
            _extractorFunctions = new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "lr", LeftRightExtractor },
                { "json", JsonExtractor },
                { "css", CssExtractor },
                { "xpath", XPathExtractor },
                { "regex", RegexExtractor }
            };
            _getCssAttributeFunctions = new Dictionary<string, Func<IElement, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "innerHTML", CssAttributeInnerHtml },
                { "outerHTML", CssAttributeOuterHtml },
                { "textContent", CssAttributeTextContent }
            };
            _getXPathAttributeFunctions = new Dictionary<string, Func<HtmlNode, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "innerHTML", XPathAttributeInnerHtml },
                { "outerHTML", XPathAttributeOuterHtml },
                { "innerText", XPathAttributeInnerText }
            };
        }

        public override Task RunAsync(BotData botData)
        {
            var result = _extractorFunctions[Type].Invoke(ReplaceValues(Source, botData)).Trim();

            if (string.IsNullOrEmpty(result))
            {
                return Task.CompletedTask;
            }

            if (IsCapture)
            {
                botData.Captures[Name] = Prefix + result + Suffix;
            }

            botData.Variables[Name] = Prefix + result + Suffix;

            return Task.CompletedTask;
        }

        private string LeftRightExtractor(string source)
        {
            var indexOfBegin = source.IndexOf(Left);

            if (indexOfBegin == -1)
            {
                return string.Empty;
            }

            source = source[(indexOfBegin + Left.Length)..];

            var indexOfEnd = source.IndexOf(Right);

            if (indexOfEnd == -1)
            {
                return string.Empty;
            }

            return source[..indexOfEnd];
        }

        private string JsonExtractor(string source)
        {
            var token = JObject.Parse(source).SelectToken(Json);

            if (token is null)
            {
                return string.Empty;
            }

            return token.ToString();
        }

        private string CssExtractor(string source)
        {
            var htmlParser = new HtmlParser();

            using var document = htmlParser.ParseDocument(source);

            var element = document.QuerySelector(Selector);

            if (element is null)
            {
                return string.Empty;
            }

            return _getCssAttributeFunctions.ContainsKey(Attribute) ? _getCssAttributeFunctions[Attribute].Invoke(element) : element.HasAttribute(Attribute) ? element.GetAttribute(Attribute) : string.Empty;
        }

        private string XPathExtractor(string source)
        {
            var htmlDocument = new HtmlDocument();
            
            htmlDocument.LoadHtml(source);
            
            var htmlNode = htmlDocument.DocumentNode.SelectSingleNode(Selector);
            
            return _getXPathAttributeFunctions.ContainsKey(Attribute) ? _getXPathAttributeFunctions[Attribute].Invoke(htmlNode) : htmlNode.GetAttributeValue(Attribute, string.Empty);
        }

        private static string CssAttributeInnerHtml(IElement element) => element.InnerHtml;

        private static string CssAttributeOuterHtml(IElement element) => element.OuterHtml;

        private static string CssAttributeTextContent(IElement element) => element.TextContent;

        private static string XPathAttributeInnerHtml(HtmlNode htmlNode) => htmlNode.InnerHtml;

        private static string XPathAttributeOuterHtml(HtmlNode htmlNode) => htmlNode.OuterHtml;

        private static string XPathAttributeInnerText(HtmlNode htmlNode) => htmlNode.InnerText;

        private string RegexExtractor(string source) => System.Text.RegularExpressions.Regex.Match(source, Regex).Groups[Group].Value;
    }
}
