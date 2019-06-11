using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BlogspotToHtmlBook.Infrastructure;
using BlogspotToHtmlBook.Model;
using HtmlAgilityPack;

namespace BlogspotToHtmlBook.Services {
    public class ScrapperService {

        private readonly string OutputFolder;
        private readonly Logger Logger;

        private string ImagesOutputFolder => $"{OutputFolder}\\images";
        private long imageFilenameKey;

        public ScrapperService(string outputFolder, Logger logger) {
            OutputFolder = outputFolder;
            Logger = logger;
        }

        public IList<BlogPost> DoScrapping(Queue<string> blogPostsUrls) {

            ClearAllContents();

            imageFilenameKey = 0;
            Directory.CreateDirectory(ImagesOutputFolder);

            if (!Directory.Exists(ImagesOutputFolder)) {
                throw new Exception($"The images folder wasn't created: { ImagesOutputFolder }");
            }

            var blogPosts = new List<BlogPost>();
            var blogpostId = 1;

            foreach (string blogPostUrl in blogPostsUrls) {
                blogPosts.Add(GetBlogPost(blogPostUrl, blogpostId));
                blogpostId++;
            }

            return blogPosts;
        }

        private void ClearAllContents() {
            DirectoryInfo di = new DirectoryInfo(OutputFolder);

            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }

            foreach (DirectoryInfo dir in di.GetDirectories()) {
                dir.Delete(true);
            }

            Logger.Log("All the contents from the output folder were deleted.");
        }

        private BlogPost GetBlogPost(string url, int blogpostId) {
            
            Logger.Log($"Getting blog post: { url }");

            HtmlWeb web = new HtmlWeb();
            HtmlDocument doc = web.Load(url);

            HtmlNode postDate = doc.DocumentNode.SelectSingleNode("//h2[@class='date-header']");

            if (postDate == null) {
                throw new ArgumentException($"Can't get post date: { url }");
            }

            HtmlNode postTitle = doc.DocumentNode.SelectSingleNode("//h3[@class='post-title entry-title']");

            if (postTitle == null) {
                throw new ArgumentException($"Can't get post title: { url }");
            }

            HtmlNode postBody = doc.DocumentNode.SelectSingleNode("//div[@class='post-body entry-content']");

            if (postBody == null) {
                throw new ArgumentException($"Can't get post body: { url }");
            }

            var postBodyWithLocalImages = LoadImagesAndChangeHtmlFromPostBody(postBody.InnerHtml);

            var post = new BlogPost(id: blogpostId, title: postTitle.InnerText, date: postDate.InnerText, bodyHtml: postBodyWithLocalImages, url: url);

            CreatePostInFileSystem(post);

            return post;
        }

        private void CreatePostInFileSystem(BlogPost post) {
            string filePath = $"{OutputFolder}\\{post.FileName}";

            if (File.Exists(filePath)) {
                throw new Exception($"Filepath already exists: { filePath }");
            }

            File.WriteAllText(filePath, post.GetPostAsHtmlPage());
        }

        private string LoadImagesAndChangeHtmlFromPostBody(string bodyHtml) {
            
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(bodyHtml);

            HtmlNodeCollection imgs = htmlDocument.DocumentNode.SelectNodes("//img");

            if (imgs != null) {

                foreach (HtmlNode img in imgs) {
                    string href = img.ParentNode.GetAttributeValue("href", null);

                    bool aTag = true;
                    if (href == null) {
                        aTag = false;

                        href = img.GetAttributeValue("src", null);
                    } else {
                        if (href.StartsWith("//")) {
                            href = "https:" + href;
                        }
                    }

                    string filename = href.Split('/').Last();
                    filename = $"{ imageFilenameKey }.{ filename.Split('.').Last() }";
                    imageFilenameKey++;

                    string filepath = $"{ ImagesOutputFolder }\\{ filename }";

                    using (WebClient client = new WebClient()) {
                        client.DownloadFile(new Uri(href), filepath);
                    }

                    if (aTag) {
                        img.ParentNode.SetAttributeValue("href", "images/" + filename); //a tag
                    }
                    img.SetAttributeValue("src", "images/" + filename); //img tag
                }

                Logger.Log($"Total images downloaded: { imgs.Count() }");

                return htmlDocument.DocumentNode.OuterHtml;
            } else {
                Logger.Log($"No images were downloaded.");

                return bodyHtml;
            }
        }
    }
}
