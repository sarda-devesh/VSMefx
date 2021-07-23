using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace VSMefx.Commands
{
    class PartInfo : Command
    {

        public PartInfo(ConfigCreator DerivedInfo, CLIOptions Arguments) : base(DerivedInfo, Arguments)
        {

        }

         /// <summary>
         /// Method to print basic information associated with all the parts in the catalog
         /// </summary>
         
        public void listAllParts()
        {
            ComposableCatalog Catalog = this.Creator.Catalog;
            foreach (ComposablePartDefinition Part in Catalog.Parts)
            {
                Console.WriteLine(GetName(Part, "[Part]"));
            }
            
        }

        
        /// <summary>
        /// Method to present detailed information about the imports/exports of a given part
        /// </summary>
        /// <param name="PartName"> The name of the part we want more information about</param>
       
        public void GetPartInfo(string PartName)
        {
            ComposablePartDefinition Definition = this.getPart(PartName); 
            if(Definition == null)
            {
                Console.WriteLine("Couldn't find part with name " + PartName);
                return;
            }
            Console.WriteLine("Printing out details for part " + PartName);
            //Print details about the exports of the given part
            foreach(var Export in Definition.ExportingMembers)
            {
                Console.WriteLine("[Export] " + Export.Key.Name);
            }
            //Print details about the parts/type the current part imports
            foreach(var Import in Definition.Imports)
            {
                Console.WriteLine("[Import] " + Import.ImportDefinition.ContractName);
            }
        }

         /// <summary> 
         /// Method to list all the exporting parts of a given type
         /// </summary>
         /// <param name="TypeName"> The type whose exporting parts we want details about </param>
        public void ListTypeExporter(string TypeName)
        {
            Console.WriteLine("Exporting parts for " + TypeName + ":");
            foreach (var Part in this.Creator.Catalog.Parts)
            { 
                foreach(var Export in Part.ExportDefinitions)
                {
                    if(Export.Value.ContractName.Equals(TypeName))
                    {
                        Console.WriteLine(GetName(Part, "[Part]"));
                    }
                }
            }
        }

        /// <summary> 
        /// Method to list all the importing parts of a given type
        /// </summary>
        /// <param name="TypeName"> The type whose importing parts we want details about </param>
        public void ListTypeImporter(string TypeName)
        {
            Console.WriteLine("Importing parts for " + TypeName + ":");
            foreach(var Part in this.Creator.Catalog.Parts)
            {
                foreach(var Import in Part.Imports)
                {
                    if(Import.ImportDefinition.ContractName.Equals(TypeName))
                    {
                        Console.WriteLine(GetName(Part, "[Part]"));
                    }
                }
            }
        }

       
    }
}
