using BlogspotToHtmlBook.Model;

namespace BlogspotToStaticWeb.Domain {
    public class AuditBlogPost {
        public BlogPost BlogPost { get; }
        public int NumberOfImages { get; }

        public AuditBlogPost(BlogPost blogPost, int numberOfImages) {
            BlogPost = blogPost;
            NumberOfImages = numberOfImages;
        }
    }
}
