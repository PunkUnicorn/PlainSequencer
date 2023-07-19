using System.Net;
using System.Net.Http;
namespace PlainSequencer.Stuff
{
    public class HttpClientProvider : IHttpClientProvider
    {
        public HttpClient Client { get; } = new HttpClient();

        public CookieContainer CookieContainer { get; } = new CookieContainer();
    }
}
