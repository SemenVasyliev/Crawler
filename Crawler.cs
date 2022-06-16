using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CrawlerLib
{
    public class Crawler
    {
        public int maxRequestCount = 10;
        public int requestCount;

        public string mainUrl;
        public HttpClient _client;
        public HashSet<string> links;

        public Crawler(string mainUrl)
        {
            this.mainUrl = mainUrl;
            links = new HashSet<string>();
            HttpClientHandler handler = new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.None };
            _client = new HttpClient(handler)
            {
                BaseAddress = new UriBuilder("https://", mainUrl).Uri
            };
        }

        public async Task<string[]> FindUrls(int depth)
        {
            requestCount = 0;
            links.Clear();
            Uri uri = new Uri("/", UriKind.Relative);
            links.Add(HttpUtility.UrlDecode(uri.ToString()));
            await ParsePage(uri, depth);
            return links.ToArray();
        }

        public async Task ParsePage(Uri path, int depth)
        {
            if (Interlocked.Increment(ref requestCount) > maxRequestCount)
                return;
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(await response.Content.ReadAsStringAsync());

            var pageLinks = htmlDoc.DocumentNode.SelectNodes("//a[@href]").Select(node => node.Attributes["href"].Value)
                .Where(link => link.Length > 0 && !link.StartsWith("#"))
                .ToList();
            List<Task> tasks = new List<Task>();
            foreach (string link in pageLinks)
            {
                try
                {
                    var uri = new Uri(link, UriKind.RelativeOrAbsolute);
                    if (uri.IsAbsoluteUri && uri.Host != mainUrl)
                    {
                        continue;
                    }
                    uri = new Uri(uri.IsAbsoluteUri ? uri.PathAndQuery : uri.ToString(), UriKind.Relative);
                    lock (links)
                    {
                        if (!links.Add(mainUrl + HttpUtility.UrlDecode(uri.ToString())))
                        {
                            continue;
                        }
                    }
                    if (depth > 0)
                        tasks.Add(ParsePage(uri, depth - 1));
                }
                catch (Exception) { }
            }
            await Task.WhenAll(tasks);
        }
    }
}
