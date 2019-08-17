using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BlogspotToHtmlBook.Infrastructure;
using BlogspotToHtmlBook.Model;
using BlogspotToHtmlBook.Services;
using HtmlAgilityPack;

namespace BlogspotToStaticWeb
{
    public class Job
    {
        private static Job _instance;

        private Job() {  }

        public static Job GetInstance() {
            if(_instance != null)
                throw new ArgumentException("Job already has an instance");

            _instance = new Job();

            return _instance;
        }

        public void CreateIndex(string outputFolder, IList<BlogPost> postCollection)
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
        
        public void CreateExternalSourcesIndex(string outputFolder, IList<BlogPost> postCollection)
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

        public void CreateFullBook(string outputFolder, IList<BlogPost> postCollection) {

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

        public void CreatePostsInFileSystem(string outputFolder, IList<BlogPost> postCollection) {

            foreach (BlogPost post in postCollection) {

                string filePath = $"{outputFolder}\\{post.FileName}";

                if (File.Exists(filePath)) {
                    throw new Exception($"Filepath already exists: { filePath }");
                }

                File.WriteAllText(filePath, post.GetPostAsHtmlPage());
            }
        }

        public void ClearAllContents(string outputFolder) {
            DirectoryInfo di = new DirectoryInfo(outputFolder);

            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories()) {
                dir.Delete(true);
            }
        }

        public void Work(string outputFolder, string bloggerRssFeed, ILogger logger)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var imagesOutputFolder = $"{outputFolder}\\images";

            Directory.CreateDirectory(imagesOutputFolder);

            if (!Directory.Exists(imagesOutputFolder)) {
                throw new Exception($"The images folder wasn't created: { imagesOutputFolder }");
            }

            ClearAllContents(outputFolder);
            logger.Debug("All the contents from the output folder were deleted.");

            var allBlogPosts = BlogspotRssService.GetAllPostsUrl(bloggerRssFeed);
            logger.Debug($"It has { allBlogPosts.Count } to download.");

            var scrapper = new ScrapperService(logger);

            var postCollection = scrapper.DoScrapping(allBlogPosts, imagesOutputFolder);
            
            CreatePostsInFileSystem(outputFolder, postCollection);
            CreateExternalSourcesIndex(outputFolder, postCollection);
            CreateFullBook(outputFolder, postCollection);
            CreateIndex(outputFolder, postCollection);

            stopwatch.Stop();

            logger.Debug($"Finished. Number of posts downloaded: {  postCollection.Count }. Total minutes: { Convert.ToInt32(stopwatch.Elapsed.TotalMinutes) }");
        }
    }
}
