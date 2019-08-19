using BlogspotToHtmlBook.Model;

namespace BlogspotToStaticWeb.Domain {
    public class AuditBlogPost {
        public readonly BlogPost BlogPost;
        public readonly int NumberOfImages;

        public AuditBlogPost(BlogPost blogPost, int numberOfImages) {
            BlogPost = blogPost;
            NumberOfImages = numberOfImages;
        }
    }
}
