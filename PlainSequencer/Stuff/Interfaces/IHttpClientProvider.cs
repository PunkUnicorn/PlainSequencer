using System.Net;
using System.Net.Http;

namespace PlainSequencer.Stuff.Interfaces
{
    public interface IHttpClientProvider
    {
        HttpClient Client { get; }
        CookieContainer CookieContainer { get; }
    }
}