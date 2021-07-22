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
        private static string[] ValidExtensions = { "dll", "exe"}; //File extensions that are considered valid 

        private List<string> AssemblyPaths { get; set; }  //Complete path of all the files we want to include in our analysis
        public ComposableCatalog catalog { get; private set; } //Catalog of all the parts we found
        public CompositionConfiguration config { get; private set; } //Configuration associated with the parts we found

        private HashSet<Regex> WhiteListExpressions { get; set; } //If using regex, store the input regular expression
        private HashSet<string> WhiteListParts { get; set; } //If not using regex, store the part names to be excluded
        private bool usingRegex { get; set; } //A boolean indicating if we are using regex or not 

        //Constants associated with the Regex's for the expressions specified in the whitelist
        private static readonly TimeSpan maxRegexTime = new TimeSpan(0, 0, 5);
        private static readonly RegexOptions options = RegexOptions.IgnoreCase;

        /*
         * <summary>
         * Method to add a given file to the list of all the assembly paths. 
         * A file is added to the list of path if it contains a valid extension and actually exists
         * </summary>
         * <returns> A boolean indicating if the file was added to the list of paths </returns>
         */
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

        /*
         * <summary>
         * Method to add valid files from the current folder and its subfolders to the list of paths
         * </summary>
         * <param name="currentPath>The complete path to the folder we want to add files from</param>
         */

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

        /*
         * <summary>
         * Method to process the input files based on whether we are using regex or not. 
         * Prints any issues encountered while processing the input file back to the user. 
         * </summary>
         * <param name = "currentFolder">The complete path to the folder that the file is present in</param>
         * <param name = "fileName">The relative path to the file from the current folder </param>
         */

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

        /*
         * <summary>
         * Method to check if a given part is present in the whitelist or not
         * </summary>
         * <param name="partName">The name of the part we want to check</param>
         * <returns> A boolean indicating if the specified part was included in the whitelist or not</returns>
         */
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

        /*
         * <summary>
         * Method to intialize the catalog and configuration objects from the input files
         * </summary>
         */

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
            //Add all the files in the input argument to the list of paths 
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
            //Add all the valid files in the input folders to the list of paths
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
            //Read and process the whitelist file, if one is present
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
