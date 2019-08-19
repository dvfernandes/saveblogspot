using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BlogspotToStaticWeb.Infrastructure {
    public class FileSystem : IStorage {
        private readonly DirectoryInfo _outputFolder;
        private Dictionary<Guid, DirectoryInfo> _subdirectories;

        public FileSystem(string outputFolder) {
            DirectoryInfo outputDirectory = new DirectoryInfo(outputFolder);

            if (!outputDirectory.Exists) {
                throw new ArgumentException($"Output folder does not exist: { outputFolder }");
            }

            //clear all contents at the folder
            foreach (FileInfo file in outputDirectory.GetFiles()) {
                file.Delete();
            }

            foreach (DirectoryInfo dir in outputDirectory.GetDirectories()) {
                dir.Delete(true);
            }

            _outputFolder = outputDirectory;
            _subdirectories = new Dictionary<Guid, DirectoryInfo>();
        }

        public async Task WriteFile(string filename, string filecontent, Guid? subdirectory) {
            try {
                var filePath = GetFilePath(filename, subdirectory);

                await File.WriteAllTextAsync(filePath, filecontent);
            }catch(Exception e) {
                throw new Exception("IStorage:FileSystem:WriteFile", e);
            }
        }

        public async Task WriteFile(string filename, byte[] filecontent, Guid? subdirectory) {
            try {
                var filePath = GetFilePath(filename, subdirectory);

                await File.WriteAllBytesAsync(filePath, filecontent);
            } catch (Exception e) {
                throw new Exception("IStorage:FileSystem:WriteFile", e);
            }
        }

        private string GetFilePath(string filename, Guid? subdirectory) {
            DirectoryInfo directory;
            if (subdirectory.HasValue) {
                if (!_subdirectories.ContainsKey(subdirectory.Value))
                    throw new ArgumentException($"Directory not found: { subdirectory.Value }");

                directory = _subdirectories[subdirectory.Value];
            } else {
                directory = _outputFolder;
            }

            string filePath = Path.Combine(directory.ToString(), filename);

            if (File.Exists(filePath)) {
                throw new Exception($"Filepath already exists: { filePath }");
            }

            return filePath;
        }

        public async Task<Guid> CreateDirectory(string name) {
            try {
                var id = Guid.NewGuid();

                _subdirectories.Add(id, _outputFolder.CreateSubdirectory(name));

                return await Task.FromResult(id);
            } catch (Exception e) {
                throw new Exception("IStorage:FileSystem:CreateDirectory", e);
            }
        }
    }
}
