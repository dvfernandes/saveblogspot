using System.Linq;

namespace BlogspotToHtmlBook.Model 
{
    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Date { get; set; }
        public string BodyHtml { get; set; }
        public string Url { get; set; }

        public string FileName { get { return Id + "-" + Url.Split('/').Last(); } }
    }
}
