namespace BlogspotToStaticWeb.Infrastructure {
    public interface IFeatures {
        bool CreateFileForEachBlogEntry { get; }
        bool CreateExternalContentIndex { get; }
    }
}
