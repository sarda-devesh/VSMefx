using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using VSMefx.Commands;

namespace VSMefx
{

    class Program
    {

        private static readonly string TestFolder = ""; //Name of the test folder to navigate to

         ///  <summary>
         ///  Configure the working directory of the current application for testing purposes based on the testFolder string. 
         /// </summary> 
        private static void SetWorkingDirectory()
        {
            string CurrentFile = Assembly.GetExecutingAssembly().Location;
            string CurrentFolder = Path.GetDirectoryName(CurrentFile);
            string RootFolder = Path.GetFullPath(Path.Combine(CurrentFolder, "..\\..\\..\\.."));
            string TestLocation = Path.Combine(RootFolder, "Tests");
            if(TestFolder.Length > 0)
            {
                TestLocation = Path.Combine(TestLocation, TestFolder); 
            }
            if (Directory.Exists(TestLocation))
            {
                Directory.SetCurrentDirectory(TestLocation);
            }
        }

        /// <summary>
        /// A command line application to diagonse composition failures in MEF applications
        /// </summary>
        /// <param name="verbose">An boolean option to toggle the detail level of the text output</param>
        /// <param name="file">Specify files from which we want to load parts from</param>
        /// <param name="directory">Specify folders from which we want to load parts from</param>
        /// <param name="parts">An boolean to toggle if we want to print out all the parts</param>
        /// <param name="detail">Specify the parts we want to get more information about</param>
        /// <param name="importer">List the parts who import the specified contract name(s)</param>
        /// <param name="exporter">List the parts who export the specified contract name(s)</param>
        /// <param name="rejected">List the rejection causes for a given part (use all to list every rejection error)</param>
        /// <param name="graph">Save a DGML graph to visualize the rejection chain</param>
        /// <param name="whitelist">A file which lists the parts we expect to be rejected</param>
        /// <param name="regex">Treat the text in the whitelist file as regular expressions</param>
        /// <param name="cache">Specify the name of the output file to store the loaded parts</param>
        /// <param name="match">Check relationship between given part which are provided in order: ExportPart ImportPart</param>
        /// <param name="matchExports">List of fields in the export part that we want to consider</param>
        /// <param name="matchImports">List of fields in the import part that we want to consider</param>
        static async Task Main(bool verbose = false, 
            List<string> file = null, 
            List<string> directory = null,
            bool parts = false,
            List<string> detail = null,
            List<string> importer = null,
            List<string> exporter = null,
            List<string> rejected = null, 
            bool graph = false, 
            string whitelist = "", 
            bool regex = false,
            string cache = "",
            List<string> match = null,
            List<string> matchExports = null,
            List<string> matchImports = null)
        {
            //SetWorkingDirectory(); 
            Console.WriteLine("Current working directory of " + Directory.GetCurrentDirectory());
            if(file != null)
            {
                foreach (var FileName in file)
                {
                    Console.WriteLine("File name of " + FileName);
                }
            }
            if(directory != null)
            {
                foreach (var FolderName in directory)
                {
                    Console.WriteLine("Folder name is " + FolderName);
                }
            }
            CLIOptions Options = new CLIOptions
            {
                Verbose = verbose,
                Files = file,
                Folders = directory,
                ListParts = parts,
                PartDetails = detail,
                ImportDetails = importer,
                ExportDetails = exporter,
                RejectedDetails = rejected,
                SaveGraph = graph,
                WhiteListFile = whitelist,
                UseRegex = regex,
                CacheFile = cache,
                MatchParts = match,
                MatchExports = matchExports,
                MatchImports = matchImports
            };
            try
            {
                await RunOptions(Options);
                Console.WriteLine("Finished Running Command");  
            } catch(Exception Error)
            {
                Console.WriteLine("Error of " + Error.Message + " with trace of " + Error.StackTrace);
            }
            Console.ReadKey();
        }
 
        /// <summary>
        /// Performs the operations and commands specified in the input arguments 
        /// </summary>        
        static async Task RunOptions(CLIOptions Options)
        {
            ConfigCreator Creator = new ConfigCreator(Options);
            await Creator.Initialize();
            if(Creator.Catalog == null)
            {
                Console.WriteLine("Couldn't find any parts in the input files and folders");
                return;
            }
            PartInfo InfoGetter = new PartInfo(Creator, Options);
            InfoGetter.PrintRequestedInfo();
            if (Options.MatchParts != null && Options.MatchParts.Count() > 0)
            {
                MatchChecker Checker = new MatchChecker(Creator, Options);
                Checker.PerformMatching();
            }
            //Perform rejection tracing as well as visualization if specified
            if (Options.RejectedDetails != null && Options.RejectedDetails.Count() > 0)
            {
                RejectionTracer Tracer = new RejectionTracer(Creator, Options);
                Tracer.PerformRejectionTracing();
            }
        }

    }
}
