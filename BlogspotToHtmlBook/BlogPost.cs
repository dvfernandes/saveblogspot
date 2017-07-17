using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogspotToHtmlBook
{
    public class BlogPost
    {
        public string Title { get; set; }
        public string Date { get; set; }
        public string BodyHtml { get; set; }
        public string FileNameOutput { get; set; }
    }
}
