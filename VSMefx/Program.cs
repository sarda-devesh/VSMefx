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
            if(options.listParts || options.partDetails.Count() > 0)
            {
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
            }
            
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            Console.WriteLine("Encountered errors");
        }
    }
}
