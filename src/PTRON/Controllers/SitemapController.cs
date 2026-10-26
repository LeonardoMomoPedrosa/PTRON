using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Linq;

namespace PTRON.Controllers;

/// <summary>
/// Generates a dynamic sitemap.xml for search engines.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class SitemapController : Controller
{
    private readonly IConfiguration _configuration;

    public SitemapController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("sitemap.xml")]
    [Produces("application/xml")]
    public IActionResult Index()
    {
        var baseUrl = _configuration["SiteUrl"] ?? "https://makelectron.com";
        
        var urls = new List<SitemapUrl>
        {
            new(baseUrl, DateTime.UtcNow, ChangeFrequency.Daily, 1.0),
            new($"{baseUrl}/login", DateTime.UtcNow, ChangeFrequency.Monthly, 0.8),
            new($"{baseUrl}/cadastro", DateTime.UtcNow, ChangeFrequency.Monthly, 0.9),
            new($"{baseUrl}/esqueci-senha", DateTime.UtcNow, ChangeFrequency.Monthly, 0.5),
        };

        var xml = GenerateSitemapXml(urls);
        return Content(xml, "application/xml", Encoding.UTF8);
    }

    private static string GenerateSitemapXml(List<SitemapUrl> urls)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        
        var sitemap = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "urlset",
                urls.Select(url => new XElement(ns + "url",
                    new XElement(ns + "loc", url.Url),
                    new XElement(ns + "lastmod", url.LastModified.ToString("yyyy-MM-dd")),
                    new XElement(ns + "changefreq", url.ChangeFrequency.ToString().ToLowerInvariant()),
                    new XElement(ns + "priority", url.Priority.ToString("F1", System.Globalization.CultureInfo.InvariantCulture))
                ))
            )
        );

        return sitemap.ToString();
    }

    private record SitemapUrl(string Url, DateTime LastModified, ChangeFrequency ChangeFrequency, double Priority);

    private enum ChangeFrequency
    {
        Always,
        Hourly,
        Daily,
        Weekly,
        Monthly,
        Yearly,
        Never
    }
}
