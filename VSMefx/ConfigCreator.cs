namespace VSMefx
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Composition;

    /// <summary>
    /// Class to store the catalog and config information for input assemblies.
    /// </summary>
    internal class ConfigCreator
    {
        /// <summary>
        /// List of file extensions that are considered valid when trying to find input files.
        /// </summary>
        /// <remarks>
        /// Ensure that the cache extension remains last since the program operates on that assumption.
        /// </remarks>
        private static readonly string[] ValidExtensions = { "dll", "exe", "cache" };

        // Constants associated with the Regex's for the expressions specified in the whitelist
        private static readonly TimeSpan MaxRegexTime = new TimeSpan(0, 0, 5);
        private static readonly RegexOptions RegexOptions = RegexOptions.IgnoreCase;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigCreator"/> class.
        /// </summary>
        /// <param name="options">The arguments inputted by the user.</param>
        public ConfigCreator(CLIOptions options)
        {
            this.AssemblyPaths = new List<string>();
            this.CachePaths = new List<string>();
            this.PartInformation = new Dictionary<string, ComposablePartDefinition>();

            // Add all the files in the input argument to the list of paths
            string currentFolder = Directory.GetCurrentDirectory();
            IEnumerable<string> files = options.Files;
            if (files != null)
            {
                foreach (string file in files)
                {
                    if (!this.AddFile(currentFolder, file))
                    {
                        Console.WriteLine("Couldn't add file " + file);
                    }
                }
            }

            // Add all the valid files in the input folders to the list of paths
            IEnumerable<string> folders = options.Folders;
            if (folders != null)
            {
                foreach (string folder in folders)
                {
                    string folderPath = Path.GetFullPath(Path.Combine(currentFolder, folder));
                    if (Directory.Exists(folderPath))
                    {
                        this.SearchFolder(folderPath);
                    }
                    else
                    {
                        Console.WriteLine("Couldn't add files from folder " + folder);
                    }
                }
            }

            // Read and process the whitelist file, if one is present
            this.UsingRegex = options.UseRegex;
            if (this.UsingRegex)
            {
                this.WhiteListExpressions = new HashSet<Regex>();
            }
            else
            {
                this.WhiteListParts = new HashSet<string>();
            }

            if (options.WhiteListFile != null && options.WhiteListFile.Length > 0)
            {
                this.ReadWhiteListFile(currentFolder, options.WhiteListFile);
            }

            this.OutputCacheFile = options.CacheFile;
        }

        /// <summary>
        /// Gets the catalog that stores information about the imported parts.
        /// </summary>
        public ComposableCatalog? Catalog { get; private set; }

        /// <summary>
        /// Gets configuration information associated with the imported parts.
        /// </summary>
        public CompositionConfiguration? Config { get; private set; }

        /// <summary>
        /// Gets or sets a dictionary storing parts indexed by thier parts name for easy lookup.
        /// </summary>
        public Dictionary<string, ComposablePartDefinition>? PartInformation { get; set; }

        /// <summary>
        /// Gets or sets the path of the cache file to store the processed parts.
        /// </summary>
        private string? OutputCacheFile { get; set; }

        /// <summary>
        /// Gets or sets the paths to the assembly files we want to read.
        /// </summary>
        private List<string> AssemblyPaths { get; set; }

        /// <summary>
        /// Gets or sets the paths to the cache files we want to read.
        /// </summary>
        private List<string> CachePaths { get; set; }

        /// <summary>
        /// Gets or sets a list of regex expression when doing whitelisting using regex.
        /// </summary>
        private HashSet<Regex>? WhiteListExpressions { get; set; }

        /// <summary>
        /// Gets or sets part names to whitelisting when not whitelisting.
        /// </summary>
        private HashSet<string>? WhiteListParts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we are using regex while whitelisting.
        /// </summary>
        private bool UsingRegex { get; set; }

        /// <summary>
        /// Method to check if a given part is present in the whitelist or not.
        /// </summary>
        /// <param name="partName">The name of the part we want to check.</param>
        /// <returns> A boolean indicating if the specified part was included in the whitelist or not.</returns>
        public bool IsWhiteListed(string partName)
        {
            if (!this.UsingRegex)
            {
                return this.WhiteListParts.Contains(partName);
            }

            foreach (Regex test in this.WhiteListExpressions)
            {
                try
                {
                    if (test.IsMatch(partName))
                    {
                        return true;
                    }
                }
                catch (Exception error)
                {
                }
            }

            return false;
        }

        /// <summary>
        /// Method to get the details about a part, i.e. the part Definition, given its name.
        /// </summary>
        /// <param name="partName"> The name of the part we want to get details about.</param>
        /// <returns>ComposablePartDefinition associated with the given part if it is present in the catalog
        ///          Null if the given part is not present in the catalog.</returns>
        public ComposablePartDefinition GetPart(string partName)
        {
            if (!this.PartInformation.ContainsKey(partName))
            {
                return null;
            }

            return this.PartInformation[partName];
        }

        /// <summary>
        /// Method to intialize the catalog and configuration objects from the input files.
        /// </summary>
        /// <returns>A Task object when all the assembly have between loaded in and configured.</returns>
        public async Task Initialize()
        {
            PartDiscovery discovery = PartDiscovery.Combine(
                new AttributedPartDiscovery(Resolver.DefaultInstance, isNonPublicSupported: true),
                new AttributedPartDiscoveryV1(Resolver.DefaultInstance));
            await this.AddToCatalog(this.AssemblyPaths, discovery);
            if (this.CachePaths.Count > 0)
            {
                await this.ReadCacheFiles(discovery);
            }

            this.PrintDiscoveryErrors();
            if (this.Catalog != null)
            {
                this.Config = CompositionConfiguration.Create(this.Catalog);
                if (this.OutputCacheFile.Length > 0)
                {
                    await this.SaveToCache();
                }

                // Add all the parts to the dictionary for lookup
                foreach (ComposablePartDefinition part in this.Catalog.Parts)
                {
                    string partName = part.Type.FullName;
                    if (!this.PartInformation.ContainsKey(partName))
                    {
                        this.PartInformation.Add(partName, part);
                    }
                }
            }
        }

        /// <summary>
        /// Method to add a given file to the list of all the assembly paths.
        /// A file is added to the list of path if it contains a valid extension and actually exists.
        /// </summary>
        /// <param name="folderPath">Path to the folder where the file is located.</param>
        /// <param name="fileName">Name of file we want to read parts from.</param>
        /// <returns> A boolean indicating if the file was added to the list of paths.</returns>
        private bool AddFile(string folderPath, string fileName)
        {
            fileName = fileName.Trim();
            int extensionIndex = fileName.LastIndexOf('.');
            bool isSucessful = false;
            if (extensionIndex >= 0)
            {
                string extension = fileName.Substring(extensionIndex + 1);
                if (ValidExtensions.Contains(extension))
                {
                    string fullPath = Path.GetFullPath(Path.Combine(folderPath, fileName));
                    if (File.Exists(fullPath))
                    {
                        bool isCacheFile = extension.Equals(ValidExtensions[ValidExtensions.Length - 1]);
                        if (isCacheFile)
                        {
                            this.CachePaths.Add(fullPath);
                        }
                        else
                        {
                            this.AssemblyPaths.Add(fullPath);
                        }

                        isSucessful = true;
                    }
                }
            }

            return isSucessful;
        }

        /// <summary>
        /// Method to add valid files from the current folder and its subfolders to the list of paths.
        /// </summary>
        /// <param name="currentPath">The complete path to the folder we want to add files from.</param>
        private void SearchFolder(string currentPath)
        {
            DirectoryInfo currentDir = new DirectoryInfo(currentPath);
            var files = currentDir.EnumerateFiles();
            foreach (var file in files)
            {
                string name = file.Name;
                this.AddFile(currentPath, name);
            }

            IEnumerable<DirectoryInfo> subFolders = currentDir.EnumerateDirectories();
            if (subFolders.Count() > 0)
            {
                foreach (DirectoryInfo subFolder in subFolders)
                {
                    this.SearchFolder(subFolder.FullName);
                }
            }
        }

        /// <summary>
        /// Method to process the input files based on whether we are using regex or not and
        /// print any issues encountered while processing the input file back to the user.
        /// </summary>
        /// <param name = "currentFolder">The complete path to the folder that the file is present in.</param>
        /// <param name = "fileName">The relative path to the file from the current folder.</param>
        private void ReadWhiteListFile(string currentFolder, string fileName)
        {
            string filePath = Path.Combine(currentFolder, fileName.Trim());
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Couldn't find whitelist file " + fileName);
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (string description in lines)
                {
                    string name = description.Trim();
                    if (this.UsingRegex)
                    {
                        string pattern = @"^" + name + @"$";
                        this.WhiteListExpressions.Add(new Regex(pattern, RegexOptions, MaxRegexTime));
                    }
                    else
                    {
                        this.WhiteListParts.Add(name);
                    }
                }
            }
            catch (Exception error)
            {
                Console.Write("Encountered error when trying to process the whitelisted file: " + error.Message);
            }
        }

        /// <summary>
        /// Method to add parts to the catalog from the given assemblies.
        /// </summary>
        /// <param name="assemblyPaths">The paths of the assemblies we want to add parts from.</param>
        /// <param name="discovery">The Part Discovery object to extract the parts from the assembly.</param>
        private async Task AddToCatalog(IEnumerable<string> assemblyPaths, PartDiscovery discovery)
        {
            if (assemblyPaths.Count() == 0)
            {
                return;
            }

            List<Assembly> assemblies = new List<Assembly>();
            foreach (var assemblyPath in assemblyPaths)
            {
                try
                {
                    var current = Assembly.LoadFrom(assemblyPath);
                    assemblies.Add(current);
                }
                catch (Exception error)
                {
                }
            }

            if (assemblies.Count > 0)
            {
                var parts = await discovery.CreatePartsAsync(assemblies);
                if (this.Catalog == null)
                {
                    this.Catalog = ComposableCatalog.Create(Resolver.DefaultInstance);
                }

                this.Catalog = this.Catalog.AddParts(parts);
            }
        }

        /// <summary>
        /// Method to read the input parts stored in cache files and add them to the existing Catalog.
        /// </summary>
        /// <param name="discovery">Part Discovery object to use when discovering parts in assembly.</param>
        private async Task ReadCacheFiles(PartDiscovery discovery)
        {
            foreach (string filePath in this.CachePaths)
            {
                try
                {
                    FileStream inputStream = File.OpenRead(filePath);
                    CachedCatalog catalogReader = new CachedCatalog();
                    ComposableCatalog cacheParts = await catalogReader.LoadAsync(inputStream, Resolver.DefaultInstance);
                    var inputAssemblies = cacheParts.GetInputAssemblies();
                    List<string> assemblyPaths = new List<string>();
                    foreach (var inputAssembly in inputAssemblies)
                    {
                        try
                        {
                            string assemblyPath = new Uri(inputAssembly.CodeBase).LocalPath;
                            assemblyPaths.Add(assemblyPath);
                        }
                        catch (Exception error)
                        {
                        }
                    }

                    await this.AddToCatalog(assemblyPaths, discovery);
                }
                catch (Exception error)
                {
                    Console.WriteLine("Encountered the following error: \"" + error.Message + "\" when trying to read " +
                        " file " + filePath);
                }
            }
        }

        /// <summary>
        /// Method to store the parts read from the input files into a cache for future use.
        /// </summary>
        private async Task SaveToCache()
        {
            string fileName = this.OutputCacheFile.Trim();
            int extensionIndex = fileName.LastIndexOf('.');
            string cacheExtension = ValidExtensions[ValidExtensions.Length - 1];
            if (extensionIndex >= 0 && fileName.Substring(extensionIndex + 1).Equals(cacheExtension))
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), fileName);
                filePath = Path.GetFullPath(filePath);
                try
                {
                    CachedCatalog cacheWriter = new CachedCatalog();
                    var fileWriter = File.Create(filePath);
                    await cacheWriter.SaveAsync(this.Catalog, fileWriter);
                    Console.WriteLine("Saved cache of current catalog to " + filePath);
                    fileWriter.Flush();
                    fileWriter.Dispose();
                }
                catch (Exception error)
                {
                    Console.WriteLine("Failed to save cache file due to error : " + error.Message);
                }
            }
            else
            {
                Console.WriteLine("Invalid file name of " + fileName);
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Method to print any discovery errors encountered during catalog creation.
        /// </summary>
        private void PrintDiscoveryErrors()
        {
            if (this.Catalog != null)
            {
                var discoveryErrors = this.Catalog.DiscoveredParts.DiscoveryErrors;
                if (discoveryErrors.Count() > 0)
                {
                    Console.WriteLine("Encountered the following errors when trying to parse input files: ");
                    discoveryErrors.ForEach(error => Console.WriteLine(error + "\n"));
                }
            }
        }
    }
}
