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

        public PartInfo(ConfigCreator DerivedInfo, CLIOptions Arguments) : base(DerivedInfo, Arguments)
        {

        }

        /*
         * <summary>
         * Method to print basic information associated with all the parts in the catalog
         * </summary>
         */
        public void listAllParts()
        {
            ComposableCatalog catalog = this.Creator.catalog;
            foreach (ComposablePartDefinition part in catalog.Parts)
            {
                Console.WriteLine(getName(part, "[Part]"));
            }
            
        }

        /*
         * <summary>
         * Method to present detailed information about the imports/exports of a given part
         * </summary>
         */
        public void getPartInfo(string partName)
        {
            ComposablePartDefinition definition = this.getPart(partName); 
            if(definition == null)
            {
                Console.WriteLine("Couldn't find part with name " + partName);
                return;
            }
            Console.WriteLine("Printing out details for part " + partName);
            //Print details about the exports of the given part
            foreach(var export in definition.ExportingMembers)
            {
                Console.WriteLine("[Export] " + export.Key.Name);
            }
            //Print details about the parts/type the current part imports
            foreach(var import in definition.Imports)
            {
                Console.WriteLine("[Import] " + import.ImportDefinition.ContractName);
            }
        }

        //TODO: Preprocess the below commands to find the exporters/importers for parts?

        /*
         * <summary> 
         * Method to list all the exporting parts of a given type
         * </summary>
         */
        public void listTypeExporter(string typeName)
        {
            Console.WriteLine("Exporting parts for " + typeName + ":");
            foreach (var part in this.Creator.catalog.Parts)
            { 
                foreach(var export in part.ExportDefinitions)
                {
                    if(export.Value.ContractName.Equals(typeName))
                    {
                        Console.WriteLine(getName(part, "[Part]"));
                    }
                }
            }
        }

        /*
         * <summary>
         * Method to detail all the parts that import a given part/type 
         * </summary>
         */

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

       
    }
}
