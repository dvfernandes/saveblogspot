using BlogspotToHtmlBook.Infrastructure;
using BlogspotToHtmlBook.Model;
using BlogspotToHtmlBook.Services;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace BlogspotToHtmlBook
{
    class Program
    {
        private static void CreateIndex(string outputFolder, IList<BlogPost> postCollection)
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

            File.WriteAllText($"{outputFolder}\\index.html", html);
        }
        
        private static void CreateExternalSourcesIndex(string outputFolder, IList<BlogPost> postCollection)
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

            File.WriteAllText($"{outputFolder}\\externalcontentindex.html", html);
        }

        private static void CreateFullBook(string outputFolder, IList<BlogPost> postCollection) {

            StringBuilder fullText = new StringBuilder("<hml><head><title>The Book</title></head><body>");
            //when priting, each post will be a new page
            fullText.Append("<style>@media print { h1 { page-break-before: always; } }</style>");

            foreach (BlogPost post in postCollection.OrderBy(p => p.Id)) {
                fullText.Append(post.GetPostAsHtml());

                fullText.Append("<hr />");
            }

            fullText.Append($"<div>Created on { DateTime.Now.ToString("MMMM d, yyyy") }</div>");

            fullText.Append("</body></hml>");

            File.WriteAllText($"{outputFolder}\\fullbook.html", fullText.ToString());
        }

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var outputFolder = ConfigurationManager.AppSettings["OutputFolder"];
            var bloggerRssFeed = ConfigurationManager.AppSettings["BloggerRssFeed"];

            var logger = new Logger();

            var allBlogPosts = BlogspotRssService.GetAllPostsUrl(bloggerRssFeed);
            logger.Log($"It has { allBlogPosts.Count } to download.");

            var scrapper = new ScrapperService(outputFolder, logger);

            var postCollection = scrapper.DoScrapping(allBlogPosts);
            
            CreateExternalSourcesIndex(outputFolder, postCollection);
            CreateFullBook(outputFolder, postCollection);
            CreateIndex(outputFolder, postCollection);

            stopwatch.Stop();

            logger.Log($"Finished. Number of posts downloaded: {  postCollection.Count }. Total minutes: { stopwatch.Elapsed.TotalMinutes }");

            Console.ReadLine();
        }
    }
}
