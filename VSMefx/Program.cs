﻿using System;
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
     * Basic Structure Information Test:
     * -p -t MefCalculator.ExportTest -e MefCalculator.MefCalculatorInterfaces+IOperation -i MajorRevision --files MefCalculator.dll  
     * -d Extensions
     * 
     * Graph Generation All:
     * -g -r all --files MefCalculator.dll -d Extensions 
     * 
     * Graph Generation Specific:
     * -gv -r ExtendedOperations.Modulo --files MefCalculator.dll -d Extensions 
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
            ConfigCreator creator = new ConfigCreator(options.files, options.folders, options.whiteListFile);
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

        static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Encountered errors");
        }
    }
}
