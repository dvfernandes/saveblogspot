using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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
    }
}
