using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace VSMefx.Commands
{
    /*
     * <summary>
     * A general command class which serves a parent class for all the commands that can be run by application 
     * </summary>
     */
    public class Command
    {
        protected ConfigCreator Creator { get; private set; } //Stores the catalog and config information for the input files 
        protected CLIOptions Options { get; private set; } //The command line arguments specified by the user 

        private Dictionary<string, ComposablePartDefinition> partInformation { get; set; }

        public Command(ConfigCreator DerivedInfo, CLIOptions Arguments)
        {
            this.Creator = DerivedInfo;
            this.Options = Arguments;
            this.partInformation = new Dictionary<string, ComposablePartDefinition>();
            foreach (ComposablePartDefinition part in Creator.catalog.Parts)
            {
                partInformation.Add(part.Type.FullName, part); 
            }
        }

        /*
         * <summary>
         * Method to get the details about a part, i.e. the part Definition, given its name.
         * </summary>
         * <param name="partName"> The name of the part we want to get details about </param>
         * <returns>ComposablePartDefinition associated with the given part if it is present in the catalog
         *          Null if the given part is not present in the catalog </returns>
         */

        protected ComposablePartDefinition getPart(string partName)
        {
            if(!this.partInformation.ContainsKey(partName))
            {
                return null;
            }
            return this.partInformation[partName]; 
        }

        /*
         * <summary>
         * Method to get the name of the given its definition
         * </summary>
         * <param name="part"> The defintion of the part whose name we want </param>
         * <returns> A string representing either the simple or verbose name of the part based
         *          on if verbose was specified as an input argument </returns>
         */ 

        protected string getName(ComposablePartDefinition part, string verboseLabel = "")
        {
            Type type = part.Type;
            if (this.Options.verbose)
            {
                return verboseLabel + " " + type.AssemblyQualifiedName;
            }
            else
            {
                return type.FullName;
            }
        }

    }

}
