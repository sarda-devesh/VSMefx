using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace VSMefx.Commands
{

    /// <summary>
    /// A general command class which serves a parent class for all the commands that can be run by application 
    /// </summary>
    class Command
    {
        protected ConfigCreator Creator { get; private set; } //Stores the catalog and config information for the input files 
        protected CLIOptions Options { get; private set; } //The command line arguments specified by the user 

        private Dictionary<string, ComposablePartDefinition> PartInformation { get; set; }

        public Command(ConfigCreator DerivedInfo, CLIOptions Arguments)
        {
            this.Creator = DerivedInfo;
            this.Options = Arguments;
            this.PartInformation = new Dictionary<string, ComposablePartDefinition>();
            if(Creator.Catalog.Parts != null)
            {
                foreach (ComposablePartDefinition part in Creator.Catalog.Parts)
                {
                    this.PartInformation.Add(part.Type.FullName, part);
                }
            }
            
        }

        
         /// <summary>
         /// Method to get the name of the given its definition
         /// </summary>
         /// <param name="Part"> The defintion of the part whose name we want </param>
         /// <param name="VerboseLabel"> Label to add before the verbose description of the part</param>
         /// <returns> A string representing either the simple or verbose name of the part based
         ///          on if verbose was specified as an input argument </returns>

        protected string GetName(ComposablePartDefinition Part, string VerboseLabel = "")
        {
            if(Part == null)
            {
                throw new ArgumentException("Request name of a null part");
            }
            Type PartType = Part.Type;
            if (this.Options.Verbose)
            {
                string Divider = " "; 
                if(VerboseLabel.Length == 0)
                {
                    Divider = "";
                }
                return VerboseLabel + Divider + PartType.AssemblyQualifiedName;
            }
            else
            {
                return PartType.FullName;
            }
        }

    }

}
