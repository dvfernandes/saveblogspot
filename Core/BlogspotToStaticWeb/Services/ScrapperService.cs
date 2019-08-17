using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using BlogspotToHtmlBook.Infrastructure;
using BlogspotToHtmlBook.Model;
using HtmlAgilityPack;

namespace BlogspotToHtmlBook.Services {
    public class ScrapperService {

        private readonly ILogger Logger;

        private long imageFilenameKey;

        public ScrapperService(ILogger logger) {
            Logger = logger;
        }

        public IList<BlogPost> DoScrapping(Queue<string> blogPostsUrls, string imagesOutputFolder) {

            imageFilenameKey = 0;

            var blogPosts = new List<BlogPost>();
            var blogpostId = 1;

            foreach (string blogPostUrl in blogPostsUrls) {
                blogPosts.Add(GetBlogPost(blogPostUrl, blogpostId, imagesOutputFolder));
                blogpostId++;
            }

            return blogPosts;
        }

        private BlogPost GetBlogPost(string url, int blogpostId, string imagesOutputFolder) {
            
            Logger.Debug($"Getting blog post: { url }");

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

            var postBodyWithLocalImages = LoadImagesAndChangeHtmlFromPostBody(postBody.InnerHtml, imagesOutputFolder);

            var post = new BlogPost(id: blogpostId, title: postTitle.InnerText, date: postDate.InnerText, bodyHtml: postBodyWithLocalImages, url: url);

            return post;
        }

        private string LoadImagesAndChangeHtmlFromPostBody(string bodyHtml, string imagesOutputFolder) {
            
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

                    string filepath = $"{ imagesOutputFolder }\\{ filename }";

                    using (WebClient client = new WebClient()) {
                        client.DownloadFile(new Uri(href), filepath);
                    }

                    if (aTag) {
                        img.ParentNode.SetAttributeValue("href", "images/" + filename); //a tag
                    }
                    img.SetAttributeValue("src", "images/" + filename); //img tag
                }

                Logger.Debug($"Total images downloaded: { imgs.Count() }");

                return htmlDocument.DocumentNode.OuterHtml;
            } else {
                Logger.Debug($"No images were downloaded.");

                return bodyHtml;
            }
        }
    }
}
