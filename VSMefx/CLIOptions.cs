namespace VSMefx
{

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /*
     * <summary>
     * Class used by Command Line Parser to load input arguments into
     * </summary>
     */
    class CLIOptions
    {

        public bool Verbose { get; set; }

        public List<string> Files { get; set; }

        public List<string> Folders { get; set; }

        public bool ListParts { get; set; }

        public List<string> PartDetails { get; set; }

        public List<string> ImportDetails { get; set; }

        public List<string> ExportDetails { get; set; }

        public List<string> RejectedDetails { get; set; }

        public bool SaveGraph { get; set; }

        public string WhiteListFile { get; set; }

        public bool UseRegex { get; set;  }

        public string CacheFile { get; set; }

        public List<string> MatchParts { get; set; }

        public List<string> MatchExports { get; set; }

        public List<string> MatchImports { get; set; }

    }
}
