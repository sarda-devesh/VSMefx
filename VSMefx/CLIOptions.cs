using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace VSMefx
{
    public class CLIOptions
    {

        [Option('v', "verbose", Default = false)]
        public bool verbose { get; set; }

        [Option('f', "files")]
        public IEnumerable<string> files { get; set; }

        [Option('d', "directories")]
        public IEnumerable<string> folders { get; set; }

        [Option('p', "parts")]
        public bool listParts { get; set; }

        [Option('t', "type")]
        public IEnumerable<string> partDetails { get; set; }

        [Option('i', "importers")]
        public IEnumerable<string> importDetails { get; set; }

        [Option('e', "exporters")]
        public IEnumerable<string> exportDetails { get; set; }

        [Option('r', "rejected")]
        public IEnumerable<string> rejectedDetails { get; set; }

        [Option('g', "graph", Default = false)]
        public bool saveGraph { get; set; }

        [Option('w', "whitelist", Default =  "" )]
        public string whiteListFile { get; set; }

        [Option('x', "regex", Default = false)]
        public bool useRegex { get; set;  }
    }
}
