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

        private static readonly string testFolder = "Basic"; //Name of the test folder to navigate to

        /*
         *  <summary>
         *  Configure the working directory of the current application for testing purposes based on the testFolder string. 
         *  </summary> 
         */ 
        private static void setWorkingDirectory()
        {
            string currentFile = Assembly.GetExecutingAssembly().Location;
            string currentFolder = Path.GetDirectoryName(currentFile);
            string rootFolder = Path.GetFullPath(Path.Combine(currentFolder, "..\\..\\.."));
            string testLocation = Path.Combine(rootFolder, "Tests");
            if(testFolder.Length > 0)
            {
                testLocation = Path.Combine(testLocation, testFolder); 
            }
            if (Directory.Exists(testLocation))
            {
                Directory.SetCurrentDirectory(testLocation);
            }
        }

        static void Main(string[] args)
        {
            setWorkingDirectory(); //TODO: Remove this call in the main application since this for the current repo's file structure
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
        
        static async Task RunOptions(CLIOptions options)
        {
            ConfigCreator creator = new ConfigCreator(options);
            await creator.Initialize();
            PartInfo infoGetter = new PartInfo(creator, options);
            //Listing all the parts present in the input files/folders
            if (options.listParts)
            {
                Console.WriteLine("Parts in Catalog are ");
                infoGetter.listAllParts();
                Console.WriteLine();
            }
            //Get more detailed information about a specific part 
            if (options.partDetails.Count() > 0)
            {
                foreach (string partName in options.partDetails)
                {
                    infoGetter.getPartInfo(partName);
                    Console.WriteLine();
                }
            }
            //Get parts that export a given type
            if(options.exportDetails.Count() > 0)
            {
                foreach(string exportType in options.exportDetails)
                {
                    infoGetter.listTypeExporter(exportType);
                    Console.WriteLine();
                }
            }
            //Get parts that import a given part or type
            if(options.importDetails.Count() > 0)
            {
                foreach(string importType in options.importDetails)
                {
                    infoGetter.listTypeImporter(importType);
                    Console.WriteLine();
                }
            }
            //Perform rejection tracing as well as visualization if specified
            if(options.rejectedDetails.Count() > 0)
            {
                RejectionTracer tracer = new RejectionTracer(creator, options);
                if(options.rejectedDetails.Contains("all"))
                {
                    tracer.listAllRejections();
                }  else
                {
                    foreach(string rejectPart in options.rejectedDetails)
                    {
                        tracer.listReject(rejectPart);
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
