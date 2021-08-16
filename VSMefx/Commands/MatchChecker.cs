using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace VSMefx.Commands
{
    class MatchChecker : Command
    {

        public MatchChecker(ConfigCreator DerivedInfo, CLIOptions Arguments) : base(DerivedInfo, Arguments)
        {
            
        }

        /// <summary>
        /// Method to get a basic description of a given constraint for output
        /// </summary>
        /// <param name="Constraint">The Constraint which we want information about</param>
        /// <returns>A string providing some details about the given constraint</returns>
        private string GetConstraintString(IImportSatisfiabilityConstraint Constraint)
        {
            //Try to treat the constraint as an indentity constraint
            if (Constraint is ExportTypeIdentityConstraint)
            {
                var IdentityConstraint = (ExportTypeIdentityConstraint)Constraint;
                return "[Type: " + IdentityConstraint.TypeIdentityName + "]";
            }
            //Try to treat the constraint as an metadata constraint
            if (Constraint is ExportMetadataValueImportConstraint)
            {
                var MetadataConstraint = (ExportMetadataValueImportConstraint)Constraint;
                return "[Metadata: " + MetadataConstraint.Name + "]";
            }
            //If it is neither just return the constraint type
            return Constraint.ToString();
        }

        /// <summary>
        /// Method to check if a export satifies the import requirements and print that result to the user
        /// </summary>
        /// <param name="Import">The ImportDefinition that we want to check against</param>
        /// <param name="Export">The ExportDefinition we want to compare with</param>
        /// <returns> 
        /// A Match Result Object indicating if there was a sucessful matches along with messages to
        /// print out to the user
        /// </returns>
        private MatchResult CheckDefinitionMatch(ImportDefinition Import, ExportDefinition Export)
        {
            MatchResult Output = new MatchResult();
            //Make sure that the contract name matches
            Output.SucessfulMatch = Import.ContractName.Equals(Export.ContractName);
            if(!Output.SucessfulMatch)
            {
                string ContractConstraint = "[Contract Name: " + Import.ContractName + "]";
                Output.Messages.Add("Export fails to sastify constraint of " + ContractConstraint);
            }
            //Check all the Import Constraints
            foreach (var Constraint in Import.ExportConstraints)
            {
                if (!Constraint.IsSatisfiedBy(Export))
                {
                    string ConstraintDetail = GetConstraintString(Constraint);
                    Output.Messages.Add("Export fails to sastify constraint of " + ConstraintDetail);
                    Output.SucessfulMatch = false;
                }
            }
            if (Output.SucessfulMatch)
            {
                Output.Messages.Add("Export matches all import constraints");
            }
            return Output;
        }

        /// <summary>
        /// Method to output to the user if the given exports satisfy the import requirements
        /// </summary>
        /// <param name="Import">The ImportDefintion we want to match against</param>
        /// <param name="MatchingExports">A list of ExportDefinitions that we want to match against the import</param>
        private void PerformDefintionChecking(ImportDefinition Import, List<PartExport> MatchingExports)
        {
            int Total = 0;
            foreach (var Export in MatchingExports)
            {
                Console.WriteLine("Considering exporting field " + Export.ExportingField);
                var Result = CheckDefinitionMatch(Import, Export.ExportDetails);
                if (Result.SucessfulMatch)
                {
                    Total += 1;
                }
                foreach (var Message in Result.Messages)
                {
                    Console.WriteLine(Message);
                }
            }
            if (MatchingExports.Count() > 1)
            {
                Console.WriteLine(Total + "/" + MatchingExports.Count() + " export(s) satisfy the import constraints");
            }
        }

        /// <summary>
        ///  Method to check if there is a relationship between two given parts
        ///  and print information regarding that match to the user
        /// </summary>
        /// <param name="ExportPart">The definition of part whose exports we want to consider</param>
        /// <param name="ImportPart">The definition whose imports we want to consider</param>
        private void CheckGeneralMatch(ComposablePartDefinition ExportPart, ComposablePartDefinition ImportPart)
        {
            //Get all the exports of the exporting part, indexed by the export contract name
            Dictionary<string, List<PartExport>> AllExportDefinitions;
            AllExportDefinitions = new Dictionary<string, List<PartExport>>();
            foreach (var Export in ExportPart.ExportDefinitions)
            {
                var ExportDetails = Export.Value;
                string ExportName = ExportDetails.ContractName;
                if (!AllExportDefinitions.ContainsKey(ExportName))
                {
                    AllExportDefinitions.Add(ExportName, new List<PartExport>());
                }
                string ExportLabel = ExportPart.Type.FullName;
                if (Export.Key != null)
                {
                    ExportLabel = Export.Key.Name;
                }
                AllExportDefinitions[ExportName].Add(new PartExport(ExportDetails, ExportLabel));

            }
            bool FoundMatch = false;
            //Find imports that have the same contract name as one of the exports and check if they match
            foreach (var Import in ImportPart.Imports)
            {
                var CurrentImportDefintion = Import.ImportDefinition;
                string CurrentContractName = CurrentImportDefintion.ContractName;
                if (AllExportDefinitions.ContainsKey(CurrentContractName))
                {
                    Console.WriteLine();
                    string FieldName = ImportPart.Type.FullName;
                    if (Import.ImportingMember != null)
                    {
                        FieldName = Import.ImportingMember.Name;
                    }
                    var PotentialMatches = AllExportDefinitions[CurrentContractName];
                    Console.WriteLine("Found potential match(es) for importing field " + FieldName);
                    FoundMatch = true;
                    PerformDefintionChecking(CurrentImportDefintion, PotentialMatches);
                }
            }
            if (!FoundMatch)
            {
                Console.WriteLine("Couldn't find any potential matches between the two given parts");
            }
        }

        /// <summary>
        /// Perform matching using the narrowd 
        /// </summary>
        /// <param name="ExportPart">The defintion of the part whose exports we want to consider</param>
        /// <param name="ImportPart">The definition of the part whose imports we want to consider</param>
        /// <param name="ExportingFields">A list of all the exporting fields we want to consider</param>
        /// <param name="ImportingFields">A list of all the importing fields we want to consider</param>
        private void CheckSpecificMatch(ComposablePartDefinition ExportPart,
            ComposablePartDefinition ImportPart,
            List<string> ExportingFields,
            List<string> ImportingFields)
        {
            List<PartExport> ConsideringExports = new List<PartExport>();
            bool IncludeAllExports = ExportingFields == null;
            //Find all the exports we want to consider during the matching phase
            foreach (var Export in ExportPart.ExportDefinitions)
            {
                var ExportDetails = Export.Value;
                string ExportLabel = ExportPart.Type.FullName;
                if (Export.Key != null)
                {
                    ExportLabel = Export.Key.Name;
                }
                bool ConsiderExport = IncludeAllExports || ExportingFields.Contains(ExportLabel);
                if (ConsiderExport)
                {
                    ConsideringExports.Add(new PartExport(ExportDetails, ExportLabel));
                    if(!IncludeAllExports)
                    {
                        ExportingFields.Remove(ExportLabel);
                    }
                }
            }
            //Print message about which exporting fields couldn't be found
            if (!IncludeAllExports && ExportingFields.Count() > 0)
            {
                Console.WriteLine("\nCouldn't find the following exporting fields: ");
                ExportingFields.ForEach(Field => Console.WriteLine(Field));
                Console.WriteLine();
            }
            //Perform matching against all considering imports
            bool CheckAllImports = ImportingFields == null;
            foreach (var Import in ImportPart.Imports)
            {
                var CurrentImportDefintion = Import.ImportDefinition;
                string ImportingField = ImportPart.Type.FullName;
                if (Import.ImportingMember != null)
                {
                    ImportingField = Import.ImportingMember.Name;
                }
                bool PerformMatching = CheckAllImports || ImportingFields.IndexOf(ImportingField) >= 0;
                if (PerformMatching)
                {
                    Console.WriteLine();
                    Console.WriteLine("Performing matching for importing field " + ImportingField); 
                    PerformDefintionChecking(CurrentImportDefintion, ConsideringExports);
                    if(!CheckAllImports)
                    {
                        ImportingFields.Remove(ImportingField);
                    }
                } 
            }
            //Print message about which importing fields couldn't be found
            if(!CheckAllImports && ImportingFields.Count() > 0)
            {
                Console.WriteLine("\nCouldn't find the following importing fields: ");
                ImportingFields.ForEach(Field => Console.WriteLine(Field));
                Console.WriteLine();
            }
        }       

        /// <summary>
        /// Method to perform matching on the input options and output the result to the user
        /// </summary>
        public void PerformMatching()
        {
            if (Options.MatchParts.Count() == 2)
            {
                string ExportPartName = Options.MatchParts.ElementAt(0).Trim();
                string ImportPartName = Options.MatchParts.ElementAt(1).Trim();
                //Deal with the case that one of the parts doesn't exist 
                ComposablePartDefinition ExportPart = Creator.GetPart(ExportPartName);
                if (ExportPart == null)
                {
                    Console.WriteLine("Couldn't find part with name " + ExportPartName);
                    return;
                }
                ComposablePartDefinition ImportPart = Creator.GetPart(ImportPartName);
                if (ImportPart == null)
                {
                    Console.WriteLine("Couldn't find part with name " + ImportPartName);
                    return;
                }
                Console.WriteLine("Finding matches from " + ExportPartName + " to " + ImportPartName);
                if(Options.MatchExports == null && Options.MatchImports == null)
                {
                    this.CheckGeneralMatch(ExportPart, ImportPart);
                } else
                {
                    this.CheckSpecificMatch(ExportPart, ImportPart, Options.MatchExports, Options.MatchImports);
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Please provide exactly two parts to the match option\n");
            }
        }

        private class PartExport
        {
            public ExportDefinition ExportDetails { get; private set; }

            public string ExportingField { get; private set; }

            public PartExport(ExportDefinition Details, string Field)
            {
                this.ExportDetails = Details;
                this.ExportingField = Field; 
            }
        }

        private class MatchResult
        {
            public bool SucessfulMatch { get; set; }

            public List<string> Messages { get; set; }

            public MatchResult()
            {
                this.SucessfulMatch = true;
                this.Messages = new List<string>();
            }

        }
    } 

}
