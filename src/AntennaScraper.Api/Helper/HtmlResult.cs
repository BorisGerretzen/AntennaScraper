using System.Net.Mime;
using System.Text;

namespace AntennaScraper.Api.Helper;

public class HtmlResult(string htmlContent) : IResult
{
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = StatusCodes.Status200OK;
        httpContext.Response.ContentType = MediaTypeNames.Text.Html;
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(htmlContent);
        await httpContext.Response.WriteAsync(htmlContent);
    }
}