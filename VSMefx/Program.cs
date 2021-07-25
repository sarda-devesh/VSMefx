using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.VisualStudio.Composition;
using VSMefx.Commands;

namespace VSMefx
{

    class Program
    {

        private static readonly string TestFolder = "Basic"; //Name of the test folder to navigate to

        /*
         *  <summary>
         *  Configure the working directory of the current application for testing purposes based on the testFolder string. 
         *  </summary> 
         */ 
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

        static void Main(string[] args)
        {
            SetWorkingDirectory(); //TODO: Remove this call in the main application since this for the current repo's file structure
            CommandLine.Parser.Default.ParseArguments<CLIOptions>(args)
            .WithParsed(async options =>
            {
                await RunOptions(options);
                Console.WriteLine("Finished running command");
            })
            .WithNotParsed(HandleParseError);
            Console.ReadKey();
        }

        /* 
         * <summary>
         * Performs the operations and commands specified in the input arguments 
         * <summary>
         */
        static async Task RunOptions(CLIOptions Options)
        {
            ConfigCreator Creator = new ConfigCreator(Options);
            await Creator.Initialize();
            PartInfo InfoGetter = new PartInfo(Creator, Options);
            //Listing all the parts present in the input files/folders
            if (Options.ListParts)
            {
                Console.WriteLine("Parts in Catalog are ");
                InfoGetter.listAllParts();
                Console.WriteLine();
            }
            //Get more detailed information about a specific part 
            if (Options.PartDetails.Count() > 0)
            {
                foreach (string PartName in Options.PartDetails)
                {
                    InfoGetter.GetPartInfo(PartName);
                    Console.WriteLine();
                }
            }
            //Get parts that export a given type
            if(Options.ExportDetails.Count() > 0)
            {
                foreach(string ExportType in Options.ExportDetails)
                {
                    InfoGetter.ListTypeExporter(ExportType);
                    Console.WriteLine();
                }
            }
            //Get parts that import a given part or type
            if(Options.ImportDetails.Count() > 0)
            {
                foreach(string ImportType in Options.ImportDetails)
                {
                    InfoGetter.ListTypeImporter(ImportType);
                    Console.WriteLine();
                }
            }
            //Perform rejection tracing as well as visualization if specified
            if(Options.RejectedDetails.Count() > 0)
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

        /*
         * <summary>
         * Display the errors from the Command Line Parser to the user 
         * </summary>
         */
        static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Encountered the following errors in parsing the input command: ");
            foreach(var error in errs)
            {
                Console.WriteLine(error);
            }
        }

    }
}
