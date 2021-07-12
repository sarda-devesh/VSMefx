using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace VSMefx.Commands
{
    public class PartInfo : Command
    {
        public static string CommandName = "PartInfo"; 

        public PartInfo(ConfigCreator DerivedInfo, CLIOptions Arguments) : base(DerivedInfo, Arguments)
        {

        }

        public void listAllParts()
        {
            ComposableCatalog catalog = this.Creator.catalog;
            foreach (ComposablePartDefinition part in catalog.Parts)
            {
                string Name = getName(part.Type, "[Part]"); 
                Console.WriteLine(Name);
            }
            
        }

        public void getPartInfo(string partName)
        {
            ComposablePartDefinition definition = this.getPart(partName); 
            if(definition == null)
            {
                Console.WriteLine("Couldn't find part with name " + partName);
                return;
            }
            Console.WriteLine("Printing out details for part " + partName);
            foreach(var export in definition.ExportingMembers)
            {
                Console.WriteLine("[Export] " + export.Key.Name);
            }
            foreach(var import in definition.Imports)
            {
                Console.WriteLine("[Import] " + import.ImportDefinition.ContractName);
            }
        }

        private string getName(Type type, string verboseLabel = "")
        {
            if(this.Options.verbose)
            {
                 return verboseLabel + " " + type.AssemblyQualifiedName;
            } else
            {
                return type.FullName;
            }
        }
    }
}
