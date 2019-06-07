using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace BlogspotToHtmlBook.Services {
    public static class BlogspotRssService {
        public static Uri GetFirstPost(string feedUrl) {
            var feed = XmlReader.Create(feedUrl);
            var rssFeed = SyndicationFeed.Load(feed);
            feed.Close();

            if (!rssFeed.Items.Any())
                throw new Exception($"Could not load any item from the RSS feed { feedUrl }");

            return rssFeed.Items.First().Links.Where(l => l.RelationshipType == "alternate" && l.MediaType == "text/html").Single().Uri;
        }

        public static Queue<string> GetAllPostsUrl(string feedUrl) {
            var posts = new Queue<string>();
            string totalResults = null;

            while (true) {
                var feed = XmlReader.Create(feedUrl);
                var rssFeed = SyndicationFeed.Load(feed);
                feed.Close();

                //Control variable for final validation
                if (totalResults == null)
                    totalResults = rssFeed.ElementExtensions.Where(e => e.OuterName == "totalResults").Single().GetObject<XElement>().Value;

                rssFeed.Items.ToList().ForEach(i => {
                    var postLink = i.Links.Where(l => l.RelationshipType == "alternate" && l.MediaType == "text/html").Single().Uri.ToString();
                    posts.Enqueue(postLink);
                });

                var nextRss = rssFeed.Links.Where(l => l.RelationshipType == "next" && l.MediaType == "application/atom+xml");
                if (!nextRss.Any())
                    break;

                feedUrl = nextRss.Single().Uri.ToString();
            }

            if (posts.Count != Convert.ToInt32(totalResults))
                throw new Exception($"I could not get the total posts I was expecting: expected - { totalResults } total - { posts.Count }");

            return posts;
        }
    }
}
