using System.Net;
using System.Net.Http;

namespace PlainSequencer.Stuff
{
    public interface IHttpClientProvider
    {
        HttpClient Client { get; }
        CookieContainer CookieContainer { get; }
    }
}