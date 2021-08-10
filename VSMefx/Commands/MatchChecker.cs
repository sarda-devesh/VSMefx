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
        /// A Tuple whose first field is a boolean indicating if the export satifies the import
        /// and whose second field is a list of messages to output to the user about the match
        /// </returns>
        private Tuple<bool, List<string>> CheckDefinitionMatch(ImportDefinition Import, ExportDefinition Export)
        {
            bool SucessfulMatch = true;
            List<string> OutputMessages = new List<string>();
            //Import = Import.AddExportConstraint(new ExportMetadataValueImportConstraint("Bound to fail", "32"));
            foreach (var Constraint in Import.ExportConstraints)
            {
                if (!Constraint.IsSatisfiedBy(Export))
                {
                    string ConstraintDetail = GetConstraintString(Constraint);
                    OutputMessages.Add("Export fails to sastify constraint of " + ConstraintDetail);
                    SucessfulMatch = false;
                }
            }
            if (SucessfulMatch)
            {
                OutputMessages.Add("Export matches all import constraints");
            }
            return new Tuple<bool, List<string>>(SucessfulMatch, OutputMessages);
        }

        /// <summary>
        /// Method to output to the user if the given exports satisfy the import requirements
        /// </summary>
        /// <param name="Import">The ImportDefintion we want to match against</param>
        /// <param name="MatchingExports">A list of ExportDefinitions that we want to match against the import</param>
        private void PerformDefintionChecking(ImportDefinition Import, List<Tuple<ExportDefinition, string>> MatchingExports)
        {
            bool PrintExportDetails = MatchingExports.Count() > 1;
            int Total = 0;
            foreach (var Export in MatchingExports)
            {
                if (PrintExportDetails)
                {
                    Console.WriteLine("Considering exporting field " + Export.Item2);
                }
                var Result = CheckDefinitionMatch(Import, Export.Item1);
                if (Result.Item1)
                {
                    Total += 1;
                }
                foreach (var Message in Result.Item2)
                {
                    Console.WriteLine(Message);
                }
            }
            bool PrintMatchSummary = PrintExportDetails;
            if (PrintMatchSummary)
            {
                Console.WriteLine(Total + "/" + MatchingExports.Count() + " export(s) satisfy the import constraints");
            }
        }

        /// <summary>
        ///  Method to check if there is a relationship between two given parts
        ///  and print information regarding that match to the user
        /// </summary>
        /// <param name="ExportPartName">The name of the part whose exports we want to consider</param>
        /// <param name="ImportPartName">The name of the part whose imports we want to consider</param>
        private void CheckMatch(string ExportPartName, string ImportPartName)
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
                return;
            }
            Console.WriteLine("Finding matches from " + ExportPartName + " to " + ImportPartName);
            //Get all the exports of the exporting part, indexed by the export contract name
            Dictionary<string, List<Tuple<ExportDefinition, string>>> AllExportDefinitions;
            AllExportDefinitions = new Dictionary<string, List<Tuple<ExportDefinition, string>>>();
            foreach (var Export in ExportPart.ExportDefinitions)
            {
                var ExportDetails = Export.Value;
                string ExportName = ExportDetails.ContractName;
                if (!AllExportDefinitions.ContainsKey(ExportName))
                {
                    AllExportDefinitions.Add(ExportName, new List<Tuple<ExportDefinition, string>>());
                }
                string ExportLabel = "Entire Part";
                if (Export.Key != null)
                {
                    ExportLabel = Export.Key.Name;
                }
                AllExportDefinitions[ExportName].Add(Tuple.Create(ExportDetails, ExportLabel));

            }
            bool FoundMatch = false;
            //Find imports that have the same contract name as one of the exports and check if they match
            foreach (var Import in ImportPart.Imports)
            {
                var CurrentImportDefintion = Import.ImportDefinition;
                string CurrentContractName = CurrentImportDefintion.ContractName;
                if (AllExportDefinitions.ContainsKey(CurrentContractName))
                {
                    string FieldName = "Part Constructor";
                    if (Import.ImportingMember != null)
                    {
                        FieldName = Import.ImportingMember.Name;
                    }
                    var PotentialMatches = AllExportDefinitions[CurrentContractName];
                    Console.WriteLine("\nFound " + PotentialMatches.Count() + " potential match(es) for importing field " + FieldName);
                    FoundMatch = true;
                    PerformDefintionChecking(CurrentImportDefintion, PotentialMatches);
                }
            }
            if (!FoundMatch)
            {
                Console.WriteLine("Couldn't find any potential matches between the two given parts");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Method to perform matching on the input options and output the result to the user
        /// </summary>
        public void PerformMatching()
        {
            if (Options.MatchParts.Count() % 2 == 0)
            {
                IEnumerable<string> ConsideringParts = Options.MatchParts;
                for (int Index = 0; Index < ConsideringParts.Count(); Index += 2)
                {
                    string ExportPart = ConsideringParts.ElementAt(Index).Trim();
                    string ImportPart = ConsideringParts.ElementAt(Index + 1).Trim();
                    this.CheckMatch(ExportPart, ImportPart);
                }
            }
            else
            {
                Console.WriteLine("Please provide an even number of part names to the match option\n");
            }
        }
    }
}
