using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BlogspotToHtmlBook.Infrastructure;
using BlogspotToHtmlBook.Model;
using BlogspotToStaticWeb.Domain;
using BlogspotToStaticWeb.Infrastructure;
using HtmlAgilityPack;

namespace BlogspotToHtmlBook.Services {
    public class ScrapperService {

        private readonly ILogger Logger;
        private readonly IStorage Storage;

        private long imageFilenameKey;

        public ScrapperService(ILogger logger, IStorage storage) {
            Logger = logger;
            Storage = storage;
        }

        public async Task<IList<BlogPost>> DoScrapping(Queue<string> blogPostsUrls, string imagesRelativePath, Guid imagesOutputFolder) {

            imageFilenameKey = 0;

            var blogPosts = new List<AuditBlogPost>();
            var blogpostId = 1;

            foreach (string blogPostUrl in blogPostsUrls) {
                var blogPost = await GetBlogPost(blogPostUrl, blogpostId, imagesRelativePath, imagesOutputFolder);
                blogPosts.Add(blogPost);
                blogpostId++;

                Logger.Debug($"The blog post with total images '{ blogPost.NumberOfImages }' was downloaded: { blogPost.BlogPost.Url }");
            }

            Logger.Debug($"Scrapping finished. Total images: '{ blogPosts.Sum(b => b.NumberOfImages) }'. Total posts: { blogPosts.Count() }");

            return blogPosts.Select(b => b.BlogPost).ToList();
        }

        private async Task<AuditBlogPost> GetBlogPost(string url, int blogpostId, string imagesRelativePath, Guid imagesOutputFolder) {
            
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

            var postBodyWithLocalImages = await LoadImagesAndChangeHtmlFromPostBody(postBody.InnerHtml, imagesRelativePath, imagesOutputFolder);

            var post = new BlogPost(id: blogpostId, title: postTitle.InnerText, date: postDate.InnerText, bodyHtml: postBodyWithLocalImages.htmlBody, url: url);

            return new AuditBlogPost(post, postBodyWithLocalImages.imageTotal);
        }

        private async Task<(string htmlBody, int imageTotal)> LoadImagesAndChangeHtmlFromPostBody(string bodyHtml, string imagesRelativePath, Guid imagesOutputFolder) {
            
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

                    using (WebClient client = new WebClient()) {
                        var imageData = await client.DownloadDataTaskAsync(new Uri(href));

                        await Storage.WriteFile(filename, imageData, imagesOutputFolder);
                    }

                    if (aTag) {
                        img.ParentNode.SetAttributeValue("href", $"{imagesRelativePath}/{filename}"); //a tag
                    }
                    img.SetAttributeValue("src", $"{imagesRelativePath}/{filename}"); //img tag
                }

                return (htmlDocument.DocumentNode.OuterHtml, imgs.Count());
            } else {
                return (bodyHtml, 0);
            }
        }
    }
}
