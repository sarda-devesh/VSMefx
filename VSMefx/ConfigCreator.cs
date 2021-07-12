using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace VSMefx
{
    public class ConfigCreator
    {
        private static string[] ValidExtensions = { "dll", "exe" };
        public List<string> AssemblyPaths { get; private set; }  
        public ComposableCatalog catalog { get; private set; }
        public CompositionConfiguration config { get; private set; }

        private bool AddFile(string folderPath, string fileName)
        {
            fileName = fileName.Trim();
            int extensionIndex = fileName.LastIndexOf('.');
            bool isSucessful = extensionIndex >= 0;
            if(isSucessful)
            {
                string extension = fileName.Substring(extensionIndex + 1);
                if (ValidExtensions.Contains(extension))
                {
                    string fullPath = Path.Combine(folderPath, fileName);
                    this.AssemblyPaths.Add(fullPath);
                    isSucessful = true;
                }
                else
                {
                    isSucessful = false;
                }
            }
            return isSucessful;
        }

        private void SearchFolder(string currentPath)
        {
            DirectoryInfo currentDir = new DirectoryInfo(currentPath);
            var files = currentDir.EnumerateFiles();
            foreach (var file in files)
            {
                string name = file.Name;
                AddFile(currentPath, name);
            }
            IEnumerable<DirectoryInfo> subFolders = currentDir.EnumerateDirectories();
            if (subFolders.Count() > 0)
            {
                foreach (DirectoryInfo subFolder in subFolders)
                {
                    SearchFolder(subFolder.FullName);
                }
            }
        }

        public async Task Initialize()
        {
            PartDiscovery discovery = PartDiscovery.Combine(
                new AttributedPartDiscovery(Resolver.DefaultInstance),
                new AttributedPartDiscoveryV1(Resolver.DefaultInstance));
            this.catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
                .AddParts(await discovery.CreatePartsAsync(this.AssemblyPaths));
            this.config = CompositionConfiguration.Create(this.catalog);
        }

        public ConfigCreator(IEnumerable<string> files, IEnumerable<string> folders)
        {
            this.AssemblyPaths = new List<string>();
            string currentFolder = Directory.GetCurrentDirectory();
            if (files != null)
            {
                foreach(string file in files)
                {
                    if(!AddFile(currentFolder, file))
                    {
                        Console.WriteLine(file + " is not a valid input file");
                    }
                }
            }
            if(folders != null)
            {
                foreach(string folder in folders)
                {
                    string folderPath = Path.Combine(currentFolder, folder);
                    SearchFolder(folderPath);
                }
            }
        }
    }
}
