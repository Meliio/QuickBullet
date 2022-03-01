using QuickBullet.Models;
using QuickBullet.Models.Blocks;
using System.Text;

namespace QuickBullet.Blocks
{
    public class BlockRequest : Block
    {
        private readonly Request _request;
        private readonly Dictionary<string, Func<BotData, HttpContent>> _messageContentGenerationFunctions;

        public BlockRequest(Request request)
        {
            _request = request;
            _messageContentGenerationFunctions = new Dictionary<string, Func<BotData, HttpContent>>(StringComparer.OrdinalIgnoreCase)
            {
                { "standard", GenerateStandardMessageContent },
                { "multipart", GenerateMultipartMessageContent }
            };
        }

        public override async Task RunAsync(BotData botData)
        {
            var requestUri = new Uri(ReplaceValues(_request.Url, botData));

            using var requestMessage = new HttpRequestMessage(_request.Method, requestUri);

            foreach (var header in _request.Headers)
            {
                requestMessage.Headers.TryAddWithoutValidation(header.Key, ReplaceValues(header.Value, botData));
            }

            botData.CookieContainer.SetCookies(requestUri, ReplaceValues(_request.CookieHeader, botData));

            var cookieHeader = botData.CookieContainer.GetCookieHeader(requestUri);

            if (!string.IsNullOrEmpty(cookieHeader))
            {
                requestMessage.Headers.Add("cookie", cookieHeader);
            }

            if (_request.StringContents.Any())
            {
                requestMessage.Content = _messageContentGenerationFunctions[_request.Type].Invoke(botData);
            }

            using var responseMessage = botData.UseProxy ? await botData.ProxyHttpClient.SendAsync(requestMessage) : await botData.HttpClient.SendAsync(requestMessage);

            botData.Variables["data.address"] = responseMessage.RequestMessage.RequestUri.AbsoluteUri;
            botData.Variables["data.responseCode"] = ((int)responseMessage.StatusCode).ToString();
            botData.Headers = responseMessage.Headers.Select(h => new KeyValuePair<string, string>(h.Key, h.Value.First()));
            if (responseMessage.Headers.TryGetValues("set-cookie", out var values))
            {
                botData.CookieContainer.SetCookies(responseMessage.RequestMessage.RequestUri, string.Join(", ", values));
            }
            botData.Variables["data.source"] = _request.LoadContent ? await responseMessage.Content.ReadAsStringAsync() : string.Empty;
        }

        private HttpContent GenerateStandardMessageContent(BotData botData) => new StringContent(ReplaceValues(_request.StringContents.First(), botData), Encoding.UTF8, _request.ContentType);

        private HttpContent GenerateMultipartMessageContent(BotData botData)
        {
            var multipartFormDataContent = new MultipartFormDataContent();

            foreach (var content in _request.StringContents)
            {
                var contentSplit = ReplaceValues(content, botData).Split(':', StringSplitOptions.TrimEntries);
                multipartFormDataContent.Add(new StringContent(contentSplit[1]), contentSplit[0]);
            }

            return multipartFormDataContent;
        }
    }
}