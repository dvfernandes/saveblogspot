using System.Linq;
using System.Text;

namespace BlogspotToHtmlBook.Model {
    public class BlogPost {

        public int Id { get; }
        public string Title { get; }
        public string Date { get; }
        public string BodyHtml { get; }
        public string Url { get; }
        public string FileName { get; }

        public BlogPost(int id, string title, string date, string bodyHtml, string url) {
            Id = id;
            Title = title;
            Date = date;
            BodyHtml = bodyHtml;
            Url = url;
            FileName = id + "-" + url.Split('/').Last();
        }

        public string GetPostAsHtml() {
            StringBuilder html = new StringBuilder($"<h1>{ Title }</h1>");
            html.Append($"<h3>{ Date }</h3>");
            html.Append($"<div>{ BodyHtml }</div>");

            return html.ToString();
        }

        public string GetPostAsHtmlPage() {
            StringBuilder html = new StringBuilder($"<hml><head><title>{ Title }</title></head><body>");
            html.Append(GetPostAsHtml());
            html.Append("</body></hml>");

            return html.ToString();
        }
    }
}
