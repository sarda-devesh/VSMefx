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

        private string GetConstraintString(IImportSatisfiabilityConstraint Constraint)
        {
            try
            {
                var IdentityConstraint = (ExportTypeIdentityConstraint) Constraint;
                return "[Type Identity: " + IdentityConstraint.TypeIdentityName + "]";
            } catch(Exception Error) { }
            try
            {
                var MetadataConstraint = (ExportMetadataValueImportConstraint) Constraint;
                return "[Export Metadata: " + MetadataConstraint.Name + "]";
            }catch(Exception Error) { }
            return Constraint.ToString();
        }

        private void CheckDefinitionMatch(ImportDefinition Import, ExportDefinition Export)
        {
            bool SucessfulMatch = true;
            foreach (var Constraint in Import.ExportConstraints)
            {
                var CastedConstraint = (ExportTypeIdentityConstraint) Constraint;
                string ConstraintDetail = GetConstraintString(Constraint);
                if (!Constraint.IsSatisfiedBy(Export))
                {
                    Console.WriteLine("Export fails to sastify constraint of " + ConstraintDetail);
                    SucessfulMatch = false;
                }
            }
            if (SucessfulMatch)
            {
                Console.WriteLine("Export matches all import contstraints");
            }   
        }

        public void CheckMatch(string ExportPartName, string ImportPartName)
        {
            ComposablePartDefinition ExportPart = GetPart(ExportPartName);
            if(ExportPart == null)
            {
                Console.WriteLine("Couldn't find part with name " + ExportPartName);
                return;
            }
            ComposablePartDefinition ImportPart = GetPart(ImportPartName);
            if(ImportPart == null)
            {
                Console.WriteLine("Couldn't find part with name " + ImportPartName);
            }
            Console.WriteLine("Finding matches between " + ExportPartName + " and " + ImportPartName);
            Dictionary<string, ExportDefinition> AllExportDefinitions = new Dictionary<string, ExportDefinition>(); 
            foreach(var Export in ExportPart.ExportDefinitions)
            {
                ExportDefinition ExportDetails = Export.Value;
                AllExportDefinitions.Add(ExportDetails.ContractName, ExportDetails);
            }
            foreach(var Import in ImportPart.Imports)
            {
                var CurrentImportDefintion = Import.ImportDefinition;
                string CurrentContractName = CurrentImportDefintion.ContractName;
                if(AllExportDefinitions.ContainsKey(CurrentContractName))
                {
                    Console.WriteLine("Found matching contract name of " + CurrentContractName);
                    var PotentialMatch = AllExportDefinitions[CurrentContractName];
                    CheckDefinitionMatch(CurrentImportDefintion, PotentialMatch);
                }
            }
            Console.WriteLine();
        }

       
    }
}
