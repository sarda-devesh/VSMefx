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
    class ConfigCreator
    {
        //Ensure that the cache extension is the last one since it needs to be processed seperately
        private static readonly string[] ValidExtensions = { "dll", "exe", "cache"}; //File extensions that are considered valid 
        private List<string> AssemblyPaths { get; set; }  //Complete path of all the files we want to include in our analysis
        
        private string OutputCacheFile { get; set; } //Path to the output file in which to store the catalog as a cache

        private List<string> CachePaths { get; set; } //Paths to all the cache files we want to process

        /// <summary>
        /// Catalog storing information about the imported parts
        /// </summary>
        public ComposableCatalog Catalog { get; private set; }

        /// <summary>
        /// Dictionary storing parts indexed by thier parts name for easy lookup
        /// </summary>
        private Dictionary<string, ComposablePartDefinition> PartInformation { get; set; }

        /// <summary>
        /// Configuration information associated with the imported parts
        /// </summary>
        public CompositionConfiguration Config { get; private set; } 

        private HashSet<Regex> WhiteListExpressions { get; set; } //If using regex, store the input regular expression
        private HashSet<string> WhiteListParts { get; set; } //If not using regex, store the part names to be excluded
        private bool UsingRegex { get; set; } //A boolean indicating if we are using regex or not 

        //Constants associated with the Regex's for the expressions specified in the whitelist
        private static readonly TimeSpan MaxRegexTime = new TimeSpan(0, 0, 5);
        private static readonly RegexOptions RegexOptions = RegexOptions.IgnoreCase;

         /// <summary>
         /// Method to add a given file to the list of all the assembly paths. 
         /// A file is added to the list of path if it contains a valid extension and actually exists
         /// </summary>
         /// <returns> A boolean indicating if the file was added to the list of paths </returns>
        private bool AddFile(string FolderPath, string FileName)
        {
            FileName = FileName.Trim();
            int ExtensionIndex = FileName.LastIndexOf('.');
            bool IsSucessful = false;
            if(ExtensionIndex >= 0)
            {
                string Extension = FileName.Substring(ExtensionIndex + 1);
                if (ValidExtensions.Contains(Extension))
                {
                    string FullPath = Path.Combine(FolderPath, FileName);
                    if(File.Exists(FullPath))
                    {
                        bool IsCacheFile = Extension.Equals(ValidExtensions[ValidExtensions.Length - 1]);
                        if (IsCacheFile)
                        {
                            this.CachePaths.Add(FullPath);
                        } else
                        {
                            this.AssemblyPaths.Add(FullPath);
                        }
                        IsSucessful = true;
                    } 
                }
            }
            return IsSucessful;
        }


        /// <summary>
        /// Method to add valid files from the current folder and its subfolders to the list of paths
        /// </summary>
        /// <param name="CurrentPath">The complete path to the folder we want to add files from</param>

        private void SearchFolder(string CurrentPath)
        {
            DirectoryInfo CurrentDir = new DirectoryInfo(CurrentPath);
            var Files = CurrentDir.EnumerateFiles();
            foreach (var File in Files)
            {
                string Name = File.Name;
                AddFile(CurrentPath, Name);
            }
            IEnumerable<DirectoryInfo> SubFolders = CurrentDir.EnumerateDirectories();
            if (SubFolders.Count() > 0)
            {
                foreach (DirectoryInfo SubFolder in SubFolders)
                {
                    SearchFolder(SubFolder.FullName);
                }
            }
        }


        /// <summary>
        /// Method to process the input files based on whether we are using regex or not. 
        /// Prints any issues encountered while processing the input file back to the user. 
        /// </summary>
        /// <param name = "CurrentFolder">The complete path to the folder that the file is present in</param>
        /// <param name = "FileName">The relative path to the file from the current folder </param>

        private void ReadWhiteListFile(string CurrentFolder, string FileName)
        {
            string FilePath = Path.Combine(CurrentFolder, FileName.Trim()); 
            if(!File.Exists(FilePath))
            {
                Console.WriteLine("Couldn't find whitelist file " + FileName);
                return;
            }
            try
            {
                string[] Lines = File.ReadAllLines(FilePath);
                foreach (string description in Lines)
                {
                    string Name = description.Trim();
                    if (this.UsingRegex)
                    {
                        string pattern = @"^" + Name + @"$";
                        this.WhiteListExpressions.Add(new Regex(pattern, RegexOptions, MaxRegexTime));
                    }
                    else
                    {
                        this.WhiteListParts.Add(Name);
                    }
                }
            }
            catch(Exception Error)
            {
                throw new Exception("Encountered error when trying to process the whitelisted file: " + Error.Message); 
            }
        }


        /// <summary>
        /// Method to check if a given part is present in the whitelist or not
        /// </summary>
        /// <param name="PartName">The name of the part we want to check</param>
        /// <returns> A boolean indicating if the specified part was included in the whitelist or not</returns>

        public bool IsWhiteListed(string PartName)
        {
            if(!this.UsingRegex)
            {
                return this.WhiteListParts.Contains(PartName);
            }
            foreach(Regex Test in this.WhiteListExpressions)
            {
                if (Test.IsMatch(PartName))
                {
                    return true;
                }
            }
            return false; 
        }

        /// <summary>
        /// Method to read the input parts stored in cache files and add them to the existing Catalog
        /// </summary>

        private async Task ReadCacheFiles()
        {
            foreach(string FilePath in this.CachePaths)
            {
                try
                {
                    FileStream InputStream = File.OpenRead(FilePath);
                    CachedCatalog CatalogReader = new CachedCatalog();
                    ComposableCatalog CurrentCatalog = await CatalogReader.LoadAsync(InputStream, Resolver.DefaultInstance);
                    if (this.Catalog == null)
                    {
                        this.Catalog = CurrentCatalog;
                    }
                    else
                    {
                        this.Catalog = this.Catalog.AddCatalog(CurrentCatalog);
                    }
                } catch(Exception Error)
                {
                    Console.WriteLine("Encountered the following error: " + Error.Message + " when trying to read " +
                        " file " + FilePath);
                }
            }
        }

        /// <summary>
        /// Method to store the parts read from the input files into a cache for future use
        /// </summary>
        private async Task SaveToCache()
        {
            string FileName = OutputCacheFile.Trim();
            int ExtensionIndex = FileName.LastIndexOf('.');
            string CacheExtension = ValidExtensions[ValidExtensions.Length - 1];
            if (ExtensionIndex >= 0 && FileName.Substring(ExtensionIndex + 1).Equals(CacheExtension))
            {
                string FilePath = Path.Combine(Directory.GetCurrentDirectory(), FileName);
                CachedCatalog CacheWriter = new CachedCatalog();
                var FileWriter = File.Create(FilePath);
                await CacheWriter.SaveAsync(this.Catalog, FileWriter);
                Console.WriteLine("Saved catalog to file " + FileName + "\n");
            }
            else
            {
                Console.WriteLine("Couldn't save catalog to file " + FileName + "\n");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Method to print any discovery errors encountered during catalog creation
        /// </summary>
        private void PrintDiscoveryErrors()
        {
            var DiscoveryErrors = this.Catalog.DiscoveredParts.DiscoveryErrors;
            if (DiscoveryErrors.Count() > 0)
            {
                Console.WriteLine("Encountered the following errors when trying to parse input files: ");
                foreach(var Error in DiscoveryErrors)
                {
                    Console.WriteLine("Encountered error of " + Error.Message + " with assembly " + Error.AssemblyPath);
                }
                Console.WriteLine();
            }
        }
        
         /// <summary>
         /// Method to intialize the catalog and configuration objects from the input files
         /// </summary>
        public async Task Initialize()
        {
            PartDiscovery Discovery = PartDiscovery.Combine(
                new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported : true),
                new AttributedPartDiscoveryV1(Resolver.DefaultInstance));
            if(this.AssemblyPaths.Count() > 0)
            {
                this.Catalog = ComposableCatalog.Create(Resolver.DefaultInstance)
                .AddParts(await Discovery.CreatePartsAsync(this.AssemblyPaths));
            }
            if(this.CachePaths.Count() > 0)
            {
                await this.ReadCacheFiles(); 
            }
            if(this.Catalog != null)
            {
                this.PrintDiscoveryErrors();
                this.Config = CompositionConfiguration.Create(this.Catalog);
                if (OutputCacheFile.Length > 0)
                {
                    await this.SaveToCache();
                }
                //Add all the parts to the dictionary for lookup
                this.PartInformation = new Dictionary<string, ComposablePartDefinition>();
                foreach (ComposablePartDefinition part in this.Catalog.Parts)
                {
                    this.PartInformation.Add(part.Type.FullName, part);
                }
            }
        }

        /// <summary>
        /// Method to get the details about a part, i.e. the part Definition, given its name.
        /// </summary>
        /// <param name="PartName"> The name of the part we want to get details about </param>
        /// <returns>ComposablePartDefinition associated with the given part if it is present in the catalog
        ///          Null if the given part is not present in the catalog </returns>
        public ComposablePartDefinition GetPart(string PartName)
        {
            if (!this.PartInformation.ContainsKey(PartName))
            {
                return null;
            }
            return this.PartInformation[PartName];
        }

        public ConfigCreator(CLIOptions Options)
        {
            this.AssemblyPaths = new List<string>();
            this.CachePaths = new List<string>();
            //Add all the files in the input argument to the list of paths 
            string CurrentFolder = Directory.GetCurrentDirectory();
            IEnumerable<string> Files = Options.Files; 
            if (Files != null)
            {
                foreach(string File in Files)
                {
                    if(!AddFile(CurrentFolder, File))
                    {
                        Console.WriteLine("Couldn't add file " + File + "\n");
                    } 
                }
            }
            //Add all the valid files in the input folders to the list of paths
            IEnumerable<string> Folders = Options.Folders; 
            if(Folders != null)
            {
                foreach(string Folder in Folders)
                {
                    string FolderPath = Path.Combine(CurrentFolder, Folder);
                    if (Directory.Exists(FolderPath))
                    {
                        SearchFolder(FolderPath);
                    } else
                    {
                        Console.WriteLine("Couldn't add files from folder " + Folder + "\n");
                    }
                    
                }
            }
            //Read and process the whitelist file, if one is present
            this.UsingRegex = Options.UseRegex;
            if (this.UsingRegex)
            {
                this.WhiteListExpressions = new HashSet<Regex>();
            }
            else
            {
                this.WhiteListParts = new HashSet<string>();
            }
            if (Options.WhiteListFile != null && Options.WhiteListFile.Length > 0)
            {
                ReadWhiteListFile(CurrentFolder, Options.WhiteListFile); 
            }
            this.OutputCacheFile = Options.CacheFile;
        }
    }
}
