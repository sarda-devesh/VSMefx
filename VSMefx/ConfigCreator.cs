using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using System.Text.RegularExpressions;

namespace VSMefx
{
    public class ConfigCreator
    {
        private static string[] ValidExtensions = { "dll", "exe" };

        public List<string> AssemblyPaths { get; private set; }  
        public ComposableCatalog catalog { get; private set; }
        public CompositionConfiguration config { get; private set; }

        private HashSet<Regex> WhiteListExpressions { get; set; }
        private HashSet<string> WhiteListParts { get; set; }
        private bool usingRegex { get; set; }

        private static readonly TimeSpan maxRegexTime = new TimeSpan(0, 0, 5);
        private static readonly RegexOptions options = RegexOptions.IgnoreCase;

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
                    if(File.Exists(fullPath))
                    {
                        this.AssemblyPaths.Add(fullPath);
                        isSucessful = true;
                    } else
                    {
                        isSucessful = false;
                    }
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

        private void ReadWhiteListFile(string currentFolder, string fileName)
        {
            string filePath = Path.Combine(currentFolder, fileName); 
            if(!File.Exists(filePath))
            {
                Console.WriteLine("Couldn't find file " + fileName);
                return;
            }
            try
            {
                string[] lines = File.ReadAllLines(filePath); 
                foreach(string description in lines)
                {
                    string name = description.Trim();
                    if(this.usingRegex)
                    {
                        string pattern = @"^" + name + @"$";
                        this.WhiteListExpressions.Add(new Regex(pattern, options, maxRegexTime));
                    } else
                    {
                        this.WhiteListParts.Add(name);
                    }
                }
            }catch(Exception e)
            {
                Console.WriteLine("Encountered error when trying to process the file: " + e.Message); 
            }
        } 

        public bool isWhiteListed(string partName)
        {
            if(!this.usingRegex)
            {
                return this.WhiteListParts.Contains(partName);
            }
            foreach(Regex test in this.WhiteListExpressions)
            {
                try
                {
                    if(test.IsMatch(partName))
                    {
                        return true;
                    }
                }catch(Exception e)
                {
                    Console.WriteLine("Encountered " + e.Message + " when testing " + partName + " against " + test.ToString());
                }
            }
            return false; 
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

        public ConfigCreator(CLIOptions options)
        {
            this.AssemblyPaths = new List<string>();
            string currentFolder = Directory.GetCurrentDirectory();
            IEnumerable<string> files = options.files; 
            if (files != null)
            {
                foreach(string file in files)
                {
                    if(!AddFile(currentFolder, file))
                    {
                        Console.WriteLine("Couldn't find file " + file);
                    }
                }
            }
            IEnumerable<string> folders = options.folders; 
            if(folders != null)
            {
                foreach(string folder in folders)
                {
                    string folderPath = Path.Combine(currentFolder, folder);
                    if (Directory.Exists(folderPath))
                    {
                        SearchFolder(folderPath);
                    } else
                    {
                        Console.WriteLine("Couldn't find folder " + folder);
                    }
                    
                }
            }
            this.usingRegex = options.useRegex;
            if (this.usingRegex)
            {
                this.WhiteListExpressions = new HashSet<Regex>();
            }
            else
            {
                this.WhiteListParts = new HashSet<string>();
            }
            if (options.whiteListFile.Length > 0)
            {
                ReadWhiteListFile(currentFolder, options.whiteListFile); 
            }
        }
    }
}
