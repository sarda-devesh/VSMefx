using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.VisualStudio.Composition;
using VSMefx.Commands;

namespace VSMefx
{

    /*
     *  [Export] MajorRevision
     *  [Export] MinorRevision
     *  [Import] MefCalculator.MefCalculatorInterfaces+ICalculator
     * 
     */


    class Program
    {
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<CLIOptions>(args)
            .WithParsed<CLIOptions>(async options =>
            {
                await RunOptions(options);
                Console.WriteLine("Finished running command");
            })
            .WithNotParsed(HandleParseError);
            Console.ReadKey();
        }
        
        static async Task RunOptions(CLIOptions options)
        {
            ConfigCreator creator = new ConfigCreator(options.files, options.folders);
            await creator.Initialize();
            PartInfo infoGetter = new PartInfo(creator, options);

            if (options.listParts)
            {
                Console.WriteLine("Parts in Catalog are ");
                infoGetter.listAllParts();
                Console.WriteLine();
            }

            if (options.partDetails.Count() > 0)
            {
                foreach (string partName in options.partDetails)
                {
                    infoGetter.getPartInfo(partName);
                    Console.WriteLine();
                }
            }

            if(options.exportDetails.Count() > 0)
            {
                foreach(string exportType in options.exportDetails)
                {
                    infoGetter.listTypeExporter(exportType);
                    Console.WriteLine();
                }
            }

            if(options.importDetails.Count() > 0)
            {
                foreach(string importType in options.importDetails)
                {
                    infoGetter.listTypeImporter(importType);
                    Console.WriteLine();
                }
            }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Encountered errors");
        }
    }
}
