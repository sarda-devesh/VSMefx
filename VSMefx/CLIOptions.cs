using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace VSMefx
{

    /*
     * <summary>
     * Class used by Command Line Parser to load input arguments into
     * </summary>
     */
    public class CLIOptions
    {

        [Option('v', "verbose", Default = false)]
        public bool Verbose { get; set; }

        [Option('f', "files")]
        public IEnumerable<string> Files { get; set; }

        [Option('d', "directories")]
        public IEnumerable<string> Folders { get; set; }

        [Option('p', "parts")]
        public bool ListParts { get; set; }

        [Option('t', "type")]
        public IEnumerable<string> PartDetails { get; set; }

        [Option('i', "importers")]
        public IEnumerable<string> ImportDetails { get; set; }

        [Option('e', "exporters")]
        public IEnumerable<string> ExportDetails { get; set; }

        [Option('r', "rejected")]
        public IEnumerable<string> RejectedDetails { get; set; }

        [Option('g', "graph", Default = false)]
        public bool SaveGraph { get; set; }

        [Option('w', "whitelist", Default =  "" )]
        public string WhiteListFile { get; set; }

        [Option('x', "regex", Default = false)]
        public bool UseRegex { get; set;  }
    }
}
