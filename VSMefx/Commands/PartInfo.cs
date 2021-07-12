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
                Console.WriteLine(getName(part, "[Part]"));
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

        public void listTypeExporter(string typeName)
        {
            Console.WriteLine("Exporting parts for " + typeName + ":");
            foreach (var part in this.Creator.catalog.Parts)
            {
                foreach(var export in part.ExportingMembers)
                {
                    if(export.Key.Name.Equals(typeName))
                    {
                        Console.WriteLine(getName(part, "[Part]"));
                    }
                }
            }
        }

        public void listTypeImporter(string typeName)
        {
            Console.WriteLine("Importing parts for " + typeName + ":");
            foreach(var part in this.Creator.catalog.Parts)
            {
                foreach(var import in part.Imports)
                {
                    if(import.ImportDefinition.ContractName.Equals(typeName))
                    {
                        Console.WriteLine(getName(part, "[Part]"));
                    }
                }
            }
        }

        private string getName(ComposablePartDefinition part, string verboseLabel = "")
        {
            Type type = part.Type;
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
