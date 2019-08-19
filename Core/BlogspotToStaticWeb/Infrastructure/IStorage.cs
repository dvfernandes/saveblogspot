using System;
using System.Threading.Tasks;

namespace BlogspotToStaticWeb.Infrastructure {
    public interface IStorage {
        Task WriteFile(string filename, string filecontent, Guid? subdirectory);
        Task WriteFile(string filename, byte[] filecontent, Guid? subdirectory);
        Task<Guid> CreateDirectory(string name);
    }
}
