using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using Newtonsoft.Json;

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
         
        private void ListAllParts()
        {
            ComposableCatalog Catalog = this.Creator.Catalog;
            foreach (var Part in Catalog.Parts)
            {
               
                Console.WriteLine(GetName(Part, "[Part]"));
            }
            
        }

        
        /// <summary>
        /// Method to present detailed information about the imports/exports of a given part
        /// </summary>
        /// <param name="PartName"> The name of the part we want more information about</param>
       
        private void GetPartInfo(string PartName)
        {
            Console.WriteLine("Printing out details for part " + PartName);
            ComposablePartDefinition Definition = Creator.GetPart(PartName); 
            if(Definition == null)
            {
                Console.WriteLine("Couldn't find part with name " + PartName);
                return;
            }
            //Print details about the exports of the given part
            foreach(var ExportPair in Definition.ExportDefinitions)
            {
                string ExportName = ExportPair.Value.ContractName;
                if(ExportPair.Key == null)
                {
                    Console.WriteLine("[Export] " + ExportName);
                } else
                {
                    string ExportField = ExportPair.Key.Name;
                    Console.WriteLine("[Export] Field: " + ExportField + ", Contract Name: " + ExportName);
                }
            }
            //Print details about the parts/type the current part imports
            foreach(var Import in Definition.Imports)
            {
                string ImportName = Import.ImportDefinition.ContractName;
                string ImportField = "Constructor";
                if (Import.ImportingMember != null)
                {
                    ImportField = Import.ImportingMember.Name;
                }
                Console.WriteLine("[Import] Field: " + ImportField + ", Contract Name: " + ImportName);
            }
        }
        
        /// <summary>
        /// Method to get a list of all the parts that contain a export with the given contract name
        /// </summary>
        /// <param name="ContractName">The contract name whose exporting parts we want</param>
        /// <returns>A list of all the parts that export the given contract name</returns>
        private List<ComposablePartDefinition> GetContractExporters(string ContractName)
        {
            List<ComposablePartDefinition> ExportingParts = new List<ComposablePartDefinition>();
            foreach(var Part in this.Creator.Catalog.Parts)
            {
                foreach (var Export in Part.ExportDefinitions)
                {
                    if (Export.Value.ContractName.Equals(ContractName))
                    {
                        ExportingParts.Add(Part);
                        break;
                    }
                }
            }
            return ExportingParts;
        }

        /// <summary> 
        /// Method to output all the exporting parts of a given contract name
        /// </summary>
        /// <param name="ContractName">The contract name whose exporters we want</param>
        private void ListTypeExporter(string ContractName)
        {
            var ExportingParts = GetContractExporters(ContractName);
            if(ExportingParts.Count() == 0)
            {
                Console.WriteLine("Couldn't find any parts exporting " + ContractName);
            } else
            {
                Console.WriteLine("Exporting parts for " + ContractName + ":");
                foreach (var Part in ExportingParts)
                {
                    Console.WriteLine(GetName(Part, "[Part]"));
                }
            }
        }

        /// <summary>
        /// Method to get a list of all the parts that contain a import with the given contract name
        /// </summary>
        /// <param name="ContractName">The contract name whose importing parts we want</param>
        /// <returns>A list of all the parts that import the given contract name</returns>
        private List<ComposablePartDefinition> GetContractImporters(string ContractName)
        {
            List<ComposablePartDefinition> ImportingParts = new List<ComposablePartDefinition>();
            foreach (var Part in this.Creator.Catalog.Parts)
            {
                foreach (var Import in Part.Imports)
                {
                    if (Import.ImportDefinition.ContractName.Equals(ContractName))
                    {
                        ImportingParts.Add(Part);
                        break;
                    }
                }
            }
            return ImportingParts;
        }

        /// <summary> 
        /// Method to output all the importing parts of a given contract name
        /// </summary>
        /// <param name="ContractName"> The contract name we want to analyze</param>
        private void ListTypeImporter(string ContractName)
        {
            var ImportingParts = GetContractImporters(ContractName);
            if(ImportingParts.Count() == 0)
            {
                Console.WriteLine("Couldn't find any parts importing " + ContractName);
            } else
            {
                Console.WriteLine("Importing parts for " + ContractName + ":");
                foreach (var Part in ImportingParts)
                {
                    Console.WriteLine(GetName(Part, "[Part]"));
                }
            }
            
        }

        /// <summary>
        /// Method to the read the arguments to the input options and output the requested info
        /// to the user
        /// </summary>
        public void PrintRequestedInfo()
        {
            //Listing all the parts present in the input files/folders
            if (Options.ListParts)
            {
                Console.WriteLine("Parts in Catalog are ");
                this.ListAllParts();
                Console.WriteLine();
            }
            //Get more detailed information about a specific part 
            if (Options.PartDetails != null && Options.PartDetails.Count() > 0)
            {
                foreach (string PartName in Options.PartDetails)
                {
                    this.GetPartInfo(PartName);
                    Console.WriteLine();
                }
            }
            //Get parts that export a given type
            if (Options.ExportDetails != null && Options.ExportDetails.Count() > 0)
            {
                foreach (string ExportType in Options.ExportDetails)
                {
                    this.ListTypeExporter(ExportType);
                    Console.WriteLine();
                }
            }
            //Get parts that import a given part or type
            if (Options.ImportDetails != null && Options.ImportDetails.Count() > 0)
            {
                foreach (string ImportType in Options.ImportDetails)
                {
                    this.ListTypeImporter(ImportType);
                    Console.WriteLine();
                }
            }
        }
       
    }
}
