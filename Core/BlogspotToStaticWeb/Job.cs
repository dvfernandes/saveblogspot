using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlogspotToHtmlBook.Infrastructure;
using BlogspotToHtmlBook.Model;
using BlogspotToHtmlBook.Services;
using BlogspotToStaticWeb.Infrastructure;
using HtmlAgilityPack;

namespace BlogspotToStaticWeb
{
    public class Job
    {
        private readonly ILogger Logger;
        private readonly IStorage Storage;
        private readonly IFeatures Features;

        public Job(ILogger logger, IStorage storage, IFeatures features) {
            Logger = logger;
            Storage = storage;
            Features = features;
        }

        public async Task CreateIndex(IList<BlogPost> postCollection)
        {
            string html = "<hml><head><title>Index</title></head><body><h1>Index</h1>";
            
            string year = null;
            foreach (BlogPost post in postCollection.OrderBy(p => p.Id))
            {
                string postYear = post.Date.Split(',').Last();
                if (year == null || year != postYear)
                {
                    year = postYear;
                    html += $"<h2>{ year }</h2>";
                }

                html += $"<b>{ post.Id }</b> - <a href='{post.FileName}'>{ post.Title }</a> - <i>{ post.Date }</i><br/>";
            }

            html += $"<p>External content from <a href='externalcontentindex.html'>here</a>.</p>";
            html += $"<p>Full book can be read <a href='fullbook.html'>here</a>.</p>";

            html += "</body></html>";

            await Storage.WriteFile("index.html", html, null);
        }
        
        public async Task CreateExternalSourcesIndex(IList<BlogPost> postCollection)
        {
            List<string> externalUrl = new List<string>();

            foreach (BlogPost post in postCollection)
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(post.BodyHtml);

                HtmlNodeCollection links = htmlDocument.DocumentNode.SelectNodes("//a");

                if (links != null)
                {
                    foreach (HtmlNode link in links)
                    {
                        string href = link.GetAttributeValue("href", null);

                        if (href == null)
                            continue;

                        if (href.EndsWith(".jpg") || href.EndsWith(".gif") || href.EndsWith(".jpeg") || href.EndsWith(".png"))
                        {
                            continue;
                        }

                        if (href.StartsWith("https:") || href.StartsWith("http:"))
                        {
                            externalUrl.Add(href);
                        }
                    }
                }
            }

            string html = "<hml><head><title>Index - External Content</title></head><body><h1>Index - External Content</h1>";

            html += "<ol>";
            foreach (string url in externalUrl)
            {
                html += $"<li><a href='{ url }' target='_blank'>{ url }</a></i></li>";
            }
            html += "</ol>";

            html += "</body></html>";

            await Storage.WriteFile("externalcontentindex.html", html, null);
        }

        public async Task CreateFullBook(IList<BlogPost> postCollection) {

            StringBuilder fullText = new StringBuilder("<hml><head><title>The Book</title></head><body>");
            //when priting, each post will be a new page
            fullText.Append("<style>@media print { h1 { page-break-before: always; } }</style>");

            foreach (BlogPost post in postCollection.OrderBy(p => p.Id)) {
                fullText.Append(post.GetPostAsHtml());

                fullText.Append("<hr />");
            }

            fullText.Append($"<div>Created on { DateTime.Now.ToString("MMMM d, yyyy") }</div>");

            fullText.Append("</body></hml>");

            await Storage.WriteFile("fullbook.html", fullText.ToString(), null);
        }

        public async Task CreatePostsInFileSystem(IList<BlogPost> postCollection) {
            foreach (BlogPost post in postCollection) {
                await Storage.WriteFile(post.FileName, post.GetPostAsHtmlPage(), null);
            }
        }

        public async Task Work(string blogspotUrl)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var allBlogPosts = BlogspotRssService.GetAllPostsUrl(blogspotUrl);
            Logger.Debug($"It has { allBlogPosts.Count } to download.");

            var scrapper = new ScrapperService(Logger, Storage);

            var imageDirectoryName = "images";
            var imageDirId = await Storage.CreateDirectory(imageDirectoryName);
            var postCollection = await scrapper.DoScrapping(allBlogPosts, imageDirectoryName, imageDirId);

            if (Features.CreateFileForEachBlogEntry) {
                await CreatePostsInFileSystem(postCollection);
                await CreateIndex(postCollection);
            }

            if (Features.CreateExternalContentIndex) {
                await CreateExternalSourcesIndex(postCollection);
            }

            await CreateFullBook(postCollection);
            
            stopwatch.Stop();

            Logger.Debug($"Finished. Number of posts downloaded: {  postCollection.Count }. Total minutes: { Convert.ToInt32(stopwatch.Elapsed.TotalMinutes) }");
        }
    }
}
