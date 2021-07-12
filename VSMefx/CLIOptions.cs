﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace VSMefx
{
    public class CLIOptions
    {

        [Option('v', "verbose")]
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

    }
}
