using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlogspotToHtmlBook
{
    class Program
    {
        private static List<BlogPost> postCollection;

        static void GetEntrance(string url)
        {
            Console.WriteLine($"Getting blog post: { url }");

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);

            HtmlNode postDate = doc.DocumentNode.SelectSingleNode("//h2[@class='date-header']");

            if (postDate == null)
            {
                throw new ArgumentException($"Can't get post date: { url }");
            }
            
            HtmlNode postTitle= doc.DocumentNode.SelectSingleNode("//h3[@class='post-title entry-title']");

            if (postTitle == null)
            {
                throw new ArgumentException($"Can't get post title: { url }");
            }

            HtmlNode postBody = doc.DocumentNode.SelectSingleNode("//div[@class='post-body entry-content']");

            if (postBody == null)
            {
                throw new ArgumentException($"Can't get post body: { url }");
            }

            BlogPost post = new BlogPost
            {
                Date = postDate.InnerText,
                Title = postTitle.InnerText,
                BodyHtml = postBody.InnerHtml,
                FileNameOutput = url.Split('/').Last()
            };

            postCollection.Add(post);

            HtmlNode nextPost = doc.DocumentNode.SelectSingleNode("//a[@id='Blog1_blog-pager-older-link']");

            if(nextPost != null)
            {
                GetEntrance(nextPost.Attributes["href"].Value);
            }
        }

        private static void CreatePosts(string outputFolder)
        {
            foreach (BlogPost post in postCollection)
            {
                string html = $"<hml><head><title>{ post.Title }</title></head><body>";

                html += $"<h1>{ post.Title }</h1>";
                html += $"<h3>{ post.Date }</h3>";
                html += $"<div>{ post.BodyHtml }</div>";

                string filePath = $"{outputFolder}\\{post.FileNameOutput}";
                do
                {
                    if (!File.Exists(filePath))
                    {
                        break;
                    }

                    post.FileNameOutput = "w" + post.FileNameOutput;
                    filePath = $"{outputFolder}\\{post.FileNameOutput}";
                } while (true);

               File.WriteAllText(filePath, html);
            }
        }

        private static void CreateIndex(string outputFolder)
        {
            string html = "<hml><head><title>Index</title></head><body><h1>Index</h1>";

            int key = 1;
            string year = null;
            foreach (BlogPost post in postCollection)
            {
                string postYear = post.Date.Split(',').Last();
                if (year == null || year != postYear)
                {
                    year = postYear;
                    html += $"<h2>{ year }</h2>";
                }

                html += $"<b>{ key }</b> - <a href='{post.FileNameOutput}'>{ post.Title }</a> - <i>{ post.Date }</i><br/>";
                key++;
            }

            html += $"<p>External content from <a href='externalcontentindex.html'>here</a>.</p>";

            html += "</body></html>";

            File.WriteAllText($"{outputFolder}\\index.html", html);
        }

        private static void ClearContentOfFolder(string folder)
        {
           DirectoryInfo di = new DirectoryInfo(folder);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private static void LoadImagesAndChangeHtml(string outputFolder)
        {
            string imgDir = $"{outputFolder}\\images";

            Directory.CreateDirectory(imgDir);
            long filenameKey = 0;
            
            foreach (BlogPost post in postCollection)
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(post.BodyHtml);

                HtmlNodeCollection imgs = htmlDocument.DocumentNode.SelectNodes("//img");

                if (imgs != null)
                {
                    Console.WriteLine($"Getting blog post images: { post.Title.Replace('\n', '\0') }");

                    foreach (HtmlNode img in imgs)
                    {
                        string href = img.ParentNode.GetAttributeValue("href", null);

                        bool aTag = true;
                        if (href == null)
                        {
                            aTag = false;

                            href = img.GetAttributeValue("src", null);
                        }
                        else
                        {
                            if (href.StartsWith("//"))
                            {
                                href = "https:" + href;
                            }
                        }
                        
                        string filename = href.Split('/').Last();
                        filename = $"{ filenameKey }.{ filename.Split('.').Last() }";
                        filenameKey++;
                        
                        string filepath = $"{ imgDir }\\{ filename }";
                        
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(new Uri(href), filepath);
                        }

                        if (aTag)
                        {
                            img.ParentNode.SetAttributeValue("href", "images/" + filename); //a tag
                        }
                        img.SetAttributeValue("src", "images/" + filename); //img tag
                    }

                    post.BodyHtml = htmlDocument.DocumentNode.OuterHtml;
                }
            }
        }

        private static void CreateExternalSourcesIndex(string outputFolder)
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

        static void Main(string[] args)
        {
            postCollection = new List<BlogPost>();

            string outputFolder = ConfigurationManager.AppSettings["OutputFolder"];

            ClearContentOfFolder(outputFolder);

            GetEntrance(ConfigurationManager.AppSettings["FirstBlogPost"]);
            LoadImagesAndChangeHtml(outputFolder);

            CreatePosts(outputFolder);

            CreateExternalSourcesIndex(outputFolder);
            CreateIndex(outputFolder);

            Console.WriteLine($"Finish. Number of posts processed: {  postCollection.Count }");
            Console.ReadLine();
        }
    }
}
