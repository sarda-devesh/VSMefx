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
         
        public void ListAllParts()
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
            ComposablePartDefinition Definition = this.GetPart(PartName); 
            if(Definition == null)
            {
                Console.WriteLine("Couldn't find part with name " + PartName);
                return;
            }
            Console.WriteLine("Printing out details for part " + PartName);
            //Print details about the exports of the given part
            foreach(var ExportPair in Definition.ExportDefinitions)
            {
                string ExportName = ExportPair.Value.ContractName;
                Console.WriteLine("[Export] " + ExportName);
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
        public List<ComposablePartDefinition> GetContractExporters(string ContractName)
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
        public void ListTypeExporter(string ContractName)
        {
            Console.WriteLine("Exporting parts for " + ContractName + ":");
            var ExportingParts = GetContractExporters(ContractName);
            foreach(var Part in ExportingParts)
            {
                Console.WriteLine(GetName(Part, "[Part]"));
            }
        }

        /// <summary>
        /// Method to get a list of all the parts that contain a import with the given contract name
        /// </summary>
        /// <param name="ContractName">The contract name whose importing parts we want</param>
        /// <returns>A list of all the parts that import the given contract name</returns>
        public List<ComposablePartDefinition> GetContractImporters(string ContractName)
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
        public void ListTypeImporter(string ContractName)
        {
            Console.WriteLine("Importing parts for " + ContractName + ":");
            var ImportingParts = GetContractImporters(ContractName);
            foreach (var Part in ImportingParts)
            {
                Console.WriteLine(GetName(Part, "[Part]"));
            }
        }

        /// <summary>
        /// Method to get a basic description of a given constraint for output
        /// </summary>
        /// <param name="Constraint">The Constraint which we want information about</param>
        /// <returns>A string providing some details about the given constraint</returns>
        private string GetConstraintString(IImportSatisfiabilityConstraint Constraint)
        {
            //Try to treat the constraint as an indentity constraint
            try
            {
                var IdentityConstraint = (ExportTypeIdentityConstraint) Constraint;
                return "[Type: " + IdentityConstraint.TypeIdentityName + "]";
            } catch(Exception Error) { }
            //Try to treat the constraint as an metadata constraint
            try
            {
                var MetadataConstraint = (ExportMetadataValueImportConstraint) Constraint;
                return "[Metadata: " + MetadataConstraint.Name + "]";
            }catch(Exception Error) { }
            //If it is neither just return the constraint type
            return Constraint.ToString();
        }

        /// <summary>
        /// Method to check if a export satifies the import requirements and print that result to the user
        /// </summary>
        /// <param name="Import">The ImportDefinition that we want to check against</param>
        /// <param name="Export">The ExportDefinition we want to compare with</param>
        private void CheckDefinitionMatch(ImportDefinition Import, ExportDefinition Export)
        {
            bool SucessfulMatch = true;
            //Import = Import.AddExportConstraint(new ExportMetadataValueImportConstraint("Bound to fail", "32"));
            foreach (var Constraint in Import.ExportConstraints)
            {
                if (!Constraint.IsSatisfiedBy(Export))
                {
                    string ConstraintDetail = GetConstraintString(Constraint);
                    Console.WriteLine("Export fails to sastify constraint of " + ConstraintDetail);
                    SucessfulMatch = false;
                }
            }
            if (SucessfulMatch)
            {
                Console.WriteLine("Export matches all import contstraints");
            }   
        }

        /// <summary>
        ///  Method to check if there is a relationship between two given parts
        ///  and print information regarding that match to the user
        /// </summary>
        /// <param name="ExportPartName">The name of the part whose exports we want to consider</param>
        /// <param name="ImportPartName">The name of the part whose imports we want to consider</param>
        public void CheckMatch(string ExportPartName, string ImportPartName)
        {
            //Deal with the case that one of the parts doesn't exist 
            ComposablePartDefinition ExportPart = GetPart(ExportPartName);
            if (ExportPart == null)
            {
                Console.WriteLine("Couldn't find part with name " + ExportPartName);
                return;
            }
            ComposablePartDefinition ImportPart = GetPart(ImportPartName);
            if (ImportPart == null)
            {
                Console.WriteLine("Couldn't find part with name " + ImportPartName);
            }
            Console.WriteLine("Finding matches between " + ExportPartName + " and " + ImportPartName);
            //Get all the exports of the exporting part, indexed by the export contract name
            Dictionary<string, ExportDefinition> AllExportDefinitions = new Dictionary<string, ExportDefinition>();
            foreach (var Export in ExportPart.ExportDefinitions)
            {
                ExportDefinition ExportDetails = Export.Value;
                AllExportDefinitions.Add(ExportDetails.ContractName, ExportDetails);
            }
            bool FoundMatch = false;
            //Find imports that have the same contract name as one of the exports and check if they match
            foreach (var Import in ImportPart.Imports)
            {
                var CurrentImportDefintion = Import.ImportDefinition;
                string CurrentContractName = CurrentImportDefintion.ContractName;
                if (AllExportDefinitions.ContainsKey(CurrentContractName))
                {
                    Console.WriteLine("Potential match with contract name " + CurrentContractName);
                    var PotentialMatch = AllExportDefinitions[CurrentContractName];
                    CheckDefinitionMatch(CurrentImportDefintion, PotentialMatch);
                    FoundMatch = true;
                }
            }
            if (!FoundMatch)
            {
                Console.WriteLine("Couldn't find any potential matches between the two given parts");
            }
            Console.WriteLine();
        }

       
    }
}
