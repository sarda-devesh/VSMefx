using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
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

        /// <param name="Verbose">An boolean option to toggle the detail level of the text output</param>
        /// <param name="File">Specify files whose parts we want to consider</param>
        /// <param name="Directory">Specify folders in which we want to look for parts</param>
        /// <param name="Parts">An boolean to toggle if we want to print out all the parts</param>
        /// <param name="Type">Specify the parts we want to get more information about</param>
        /// <param name="Importer">List the parts who import the specified contract name(s)</param>
        /// <param name="Exporter">List the parts who export the specified contract name(s)</param>
        /// <param name="Rejected">List the rejection causes for a given part (use all if you want all the rejection errors)</param>
        /// <param name="Graph">Save a DGML graph to visualize the rejection chain</param>
        /// <param name="Whitelist">A file which lists the parts we expect to be rejected</param>
        /// <param name="Regex">A boolean to toggle if we want to treat the text in the whitelist file as regular expressions</param>
        /// <param name="Cache">Specify the name of the output file if we want to store the input files in a cache</param>
        /// <returns></returns>
        static async Task Main(bool Verbose = false, 
            List<string> File = null, 
            List<string> Directory = null,
            bool Parts = false,
            List<string> Type = null,
            List<string> Importer = null,
            List<string> Exporter = null,
            List<string> Rejected = null, 
            bool Graph = false, 
            string Whitelist = "", 
            bool Regex = false,
            string Cache = "")
        {
            try
            {
                SetWorkingDirectory(); //TODO: Remove this call in the main application since this for the current repo's file structure
                CLIOptions Options = new CLIOptions();
                Options.Verbose = Verbose; Options.Files = File; Options.Folders = Directory; Options.ListParts = Parts;
                Options.PartDetails = Type; Options.ImportDetails = Importer; Options.ExportDetails = Exporter;
                Options.RejectedDetails = Rejected; Options.SaveGraph = Graph; Options.WhiteListFile = Whitelist; 
                Options.UseRegex = Regex; Options.CacheFile = Cache;
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
        /// <summary>        
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
