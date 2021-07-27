using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSMefx
{

    /*
     * <summary>
     * Class used by Command Line Parser to load input arguments into
     * </summary>
     */
    class CLIOptions
    {

        public bool Verbose { get; set; }

        public IEnumerable<string> Files { get; set; }

        public IEnumerable<string> Folders { get; set; }

        public bool ListParts { get; set; }

        public IEnumerable<string> PartDetails { get; set; }

        public IEnumerable<string> ImportDetails { get; set; }

        public IEnumerable<string> ExportDetails { get; set; }

        public IEnumerable<string> RejectedDetails { get; set; }

        public bool SaveGraph { get; set; }

        public string WhiteListFile { get; set; }

        public bool UseRegex { get; set;  }

        public string CacheFile { get; set; }
    }
}
