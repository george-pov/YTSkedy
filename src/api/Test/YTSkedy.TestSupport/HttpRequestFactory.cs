using System.Text;
using Microsoft.AspNetCore.Http;

namespace YTSkedy.TestSupport;

public static class HttpRequestFactory
{
    public static HttpRequest WithBody(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context.Request;
    }
}
