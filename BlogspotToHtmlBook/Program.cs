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
        private static Stack<BlogPost> postCollection;

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

            postCollection.Push(post);

            HtmlNode nextPost = doc.DocumentNode.SelectSingleNode("//a[@id='Blog1_blog-pager-older-link']");

            if(nextPost != null)
            {
                GetEntrance(nextPost.Attributes["href"].Value);
            }
        }

        private static void RemoveDuplicateFilenames(string outputFolder)
        {
            foreach(BlogPost post in postCollection)
            {
                while (true)
                {
                    if (!File.Exists($"{outputFolder}\\{post.FileNameOutput}"))
                    {
                        break;
                    }

                    post.FileNameOutput = "p" + post.FileNameOutput;
                }
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

                File.WriteAllText($"{outputFolder}\\{post.FileNameOutput}", html);
            }
        }

        private static void CreateIndex(string outputFolder)
        {
            string html = "<hml><head><title>Index</title></head><body>";

            int key = 1;
            foreach (BlogPost post in postCollection)
            {
                html += $"<b>{ key }</b> - <a href='{outputFolder}\\{post.FileNameOutput}'>{ post.Title }</a> - <i>{ post.Date }</i><br/>";
                key++;
            }

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

            foreach (BlogPost post in postCollection)
            {
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(post.BodyHtml);

                HtmlNodeCollection imgs = htmlDocument.DocumentNode.SelectNodes("//img");

                if (imgs != null)
                {
                    Console.WriteLine($"Getting blog post images: { post.Title }");

                    foreach (HtmlNode img in imgs)
                    {
                        string href = img.ParentNode.GetAttributeValue("href", null);

                        if (href.StartsWith("//"))
                        {
                            href = "https:" + href;
                        }

                        string filename = $"{ imgDir }\\{ href.Replace('/', '.').Replace(':', '.') }";
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(new Uri(href), filename);
                        }

                        img.ParentNode.SetAttributeValue("href", "images/" + Path.GetFileName(filename));
                        img.SetAttributeValue("src", "images/" + Path.GetFileName(filename));
                    }

                    post.BodyHtml = htmlDocument.DocumentNode.OuterHtml;
                }
            }
        }

        static void Main(string[] args)
        {
            postCollection = new Stack<BlogPost>();

            string outputFolder = ConfigurationManager.AppSettings["OutputFolder"];

            ClearContentOfFolder(outputFolder);

            GetEntrance(ConfigurationManager.AppSettings["FirstBlogPost"]);

            RemoveDuplicateFilenames(outputFolder);

            CreatePosts(outputFolder);
            LoadImagesAndChangeHtml(outputFolder);
            CreateIndex(outputFolder);

            Console.WriteLine($"Finish. Number of posts processed: {  postCollection.Count }");
            Console.ReadLine();
        }
    }
}
