namespace BlogspotToStaticWeb.Infrastructure {
    public class Features : IFeatures {
        public bool CreateFileForEachBlogEntry { get; }
        public bool CreateExternalContentIndex{ get; }

        public Features(bool createFileForEachBlogEntry, bool createExternalContentIndex) {
            CreateFileForEachBlogEntry = createFileForEachBlogEntry;
            CreateExternalContentIndex = createExternalContentIndex;
        }
    }
}
