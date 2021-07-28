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

        private static readonly string TestFolder = "Basic"; //Name of the test folder to navigate to

         ///  <summary>
         ///  Configure the working directory of the current application for testing purposes based on the testFolder string. 
         /// </summary> 
        private static void SetWorkingDirectory()
        {
            string CurrentFile = Assembly.GetExecutingAssembly().Location;
            string CurrentFolder = Path.GetDirectoryName(CurrentFile);
            string RootFolder = Path.GetFullPath(Path.Combine(CurrentFolder, "..\\..\\.."));
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
        /// <param name="regex">A boolean to toggle if we want to treat the text in the whitelist file as regular expressions</param>
        /// <param name="cache">Specify the name of the output file to store the loaded parts</param>
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
            string cache = "")
        {
            try
            {
                SetWorkingDirectory(); //TODO: Remove this call in the main application since this for the current repo's file structure
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
                    CacheFile = cache
                };
                await RunOptions(Options);
                Console.WriteLine("Finished Running Command");
            } catch(Exception e)
            {
                Console.WriteLine("Error of " + e.Message + " having trace of " + e.StackTrace);
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
            if(Options.CacheFile.Length > 0)
            {
                await Creator.SaveToCache(Options.CacheFile);
            }
            PartInfo InfoGetter = new PartInfo(Creator, Options);
            //Listing all the parts present in the input files/folders
            if (Options.ListParts)
            {
                Console.WriteLine("Parts in Catalog are ");
                InfoGetter.ListAllParts();
                Console.WriteLine();
            }
            //Get more detailed information about a specific part 
            if (Options.PartDetails != null && Options.PartDetails.Count() > 0)
            {
                foreach (string PartName in Options.PartDetails)
                {
                    InfoGetter.GetPartInfo(PartName);
                    Console.WriteLine();
                }
            }
            //Get parts that export a given type
            if(Options.ExportDetails != null && Options.ExportDetails.Count() > 0)
            {
                foreach(string ExportType in Options.ExportDetails)
                {
                    InfoGetter.ListTypeExporter(ExportType);
                    Console.WriteLine();
                }
            }
            //Get parts that import a given part or type
            if(Options.ImportDetails != null && Options.ImportDetails.Count() > 0)
            {
                foreach(string ImportType in Options.ImportDetails)
                {
                    InfoGetter.ListTypeImporter(ImportType);
                    Console.WriteLine();
                }
            }
            //Perform rejection tracing as well as visualization if specified
            if(Options.RejectedDetails != null && Options.RejectedDetails.Count() > 0)
            {
                RejectionTracer Tracer = new RejectionTracer(Creator, Options);
                if(Options.RejectedDetails.Contains("all"))
                {
                    Tracer.ListAllRejections();
                }  else
                {
                    foreach(string RejectPart in Options.RejectedDetails)
                    {
                        Tracer.ListReject(RejectPart);
                    }
                }
                
            }
        }

    }
}
