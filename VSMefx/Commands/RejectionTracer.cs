using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using OpenSoftware.DgmlTools.Model;

namespace VSMefx.Commands
{
    public class RejectionTracer : Command
    {
        
         /// <summary>
         /// All the nodes in the rejectionGraph, which is a graph representation of 
         /// the error stack provided by the config 
         /// </summary>
        private Dictionary<string, PartNode> RejectionGraph { get; set; } 

        
        /// <summary>
        /// The number of levels present in the overall graph where the level value
        /// corresponds to the depth of the node/part in the error stack
        /// </summary>
        private int MaxLevels { get; set; } 
        
        /// <summary>
        /// Method to initialize the part nodes and thier "pointers" based on the error
        /// stack from the config
        /// </summary>
         
        private void GenerateNodeGraph()
        {
            //Get the error stack from the composition configuration
            CompositionConfiguration Config = this.Creator.Config;
            var Errors = Config.CompositionErrors;
            int LevelNumber = Errors.Count();
            this.MaxLevels = LevelNumber;
            Console.WriteLine("Generating error graph from composition errors"); 
            while (Errors.Count() > 0)
            {
                //Process all the parts present in the current level of the stack
                var CurrentLevel = Errors.Peek();
                Console.WriteLine("Proccesing elements in level " + LevelNumber); 
                foreach (var Element in CurrentLevel)
                {
                    var part = Element.Parts.First();
                    Console.WriteLine("Currently processing part " + part.Definition.Type.FullName);
                    Console.WriteLine("Element Message is " + Element.Message);
                    //Create a PartNode object from the definition of the current Part
                    ComposablePartDefinition Definition = part.Definition;
                    PartNode CurrentNode = new PartNode(Definition, Element.Message, LevelNumber);
                    string CurrentName = CurrentNode.GetName();
                    CurrentNode.SetWhiteListed(this.Creator.isWhiteListed(CurrentName));
                    //Get the imports for the current part to update the pointers associated with the current node
                    var Imports = part.Definition.Imports;
                    foreach (var Import in Imports)
                    {
                        string ImportName = Import.ImportDefinition.ContractName;
                        Console.WriteLine("The import name is " + ImportName);
                        
                        // As stated in the documentation for CompositionConfiguration, errors near the top of the stack
                        // cause errors in elements near the point of the stack. Thus, we check the graph for the import
                        // since any import that has rejection issues would have already been processed. 
                         
                        if (RejectionGraph.ContainsKey(ImportName))
                        {
                            PartNode ChildNode = RejectionGraph[ImportName];
                            ChildNode.AddParent(CurrentNode); //Make the current node a "parent" of the import Node
                            CurrentNode.AddChild(ChildNode); //Make the import node a "child" of the current Node
                        }
                    }
                    foreach(var Export in part.Definition.ExportDefinitions)
                    {
                        Console.WriteLine("Export is " + Export.Value.ContractName);
                    }
                    //If we have already processed the current part before - potential error with the rejection stack
                    if (RejectionGraph.ContainsKey(CurrentName))
                    {
                        throw new Exception("Node already present in graph, potential issue with duplicates");
                    }
                    RejectionGraph.Add(CurrentName, CurrentNode);
                }
                //Get the next level of the stack
                Errors = Errors.Pop();
                LevelNumber -= 1;
            }
            Console.WriteLine();
        }

        public RejectionTracer(ConfigCreator DerivedInfo, CLIOptions Arguments) : base(DerivedInfo, Arguments)
        {
            this.RejectionGraph = new Dictionary<string, PartNode>();
            this.GenerateNodeGraph();
        }

        
        /// <summary>
        /// Method to indicate all the rejection issues present in a given level
        /// </summary>
        /// <param name="CurrentLevel">An integer representing the level we are intrested in</param>
        private void ListErrorsinLevel(int CurrentLevel)
        {
            Console.WriteLine("Listing errors in level " + CurrentLevel);
            foreach(var Pair in this.RejectionGraph)
            {
                PartNode CurrentNode = Pair.Value;
                if(CurrentNode.Level.Equals(CurrentLevel))
                {
                    WriteNodeDetail(CurrentNode);
                    
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Method to display all the rejections present in the input files.
        /// If the graph argument was passed in the input arguments, a DGML graph representing
        /// all the rejection issues is saved in a file called All.dgml
        /// </summary>
        /// <remarks>
        /// Based on how the levels are assigned, the root causes for the errors in the 
        /// application can easily be accessed by looking at the rejection issues present at
        /// the highest level. 
        /// </remarks>

        public void ListAllRejections ()
        {
            for(int Level = MaxLevels; Level > 0; Level--)
            {
                ListErrorsinLevel(Level);
            }
            if(Options.SaveGraph)
            {
                GraphCreator creater = new GraphCreator(RejectionGraph);
                creater.SaveGraph("All.dgml");
            }
        }

        
        /// <summary>
        /// Method to the get the information about the rejection information that caused a
        /// particular import failure, rather than for the entire system.
        /// If graph was specified in the input arguments then a DGML graph tracing the rejection
        /// chain assocaited with the current path alone is saved to a file called [partName].dgml
        /// </summary>
        /// <param name = "PartName"> The name of the part which we want to analyze</param>
        /// <remarks>
        /// Once again, the root causes can easily be accessed by looking at the rejection
        /// issues at the highest levels of the output. 
        /// </remarks>
        
        public void ListReject(string PartName)
        {
            //Deal with the case that there are no rejection issues with the given part
            if (!RejectionGraph.ContainsKey(PartName))
            {
                Console.WriteLine("No Rejection Issues associated with " + PartName + "\n");
                return;
            }
            Console.WriteLine("Printing Rejection Graph Info for " + PartName + "\n");
            //Store just the nodes that are involved in the current rejection chain to use when generating the graph
            Dictionary<string, PartNode> RelevantNodes = null; 
            if (Options.SaveGraph)
            {
                RelevantNodes = new Dictionary<string, PartNode>();
            }
            
            // Perform Breadth First Search (BFS) with the node associated with partName as the root. 
            // When performing BFS, only the child nodes are considered since we want to the root to be
            // the end point of the rejection chain(s). 
            // BFS was chosen over DFS because of the fact that we process level by level when performing
            // the travesal and thus easier to communicate the causes and pathway to the end user

            Queue<PartNode> CurrentLevelNodes = new Queue<PartNode>();
            CurrentLevelNodes.Enqueue(RejectionGraph[PartName]);
            int CurrentLevel = 1; 
            while(CurrentLevelNodes.Count() > 0)
            {
                Console.WriteLine("Errors in Level " + CurrentLevel);
                //Iterate through all the nodes in the current level
                int NumNodes = CurrentLevelNodes.Count();
                for (int Index = 0; Index < NumNodes; Index++)
                {
                    //Process the current node by displaying its import issue and adding it to the graph
                    PartNode Current = CurrentLevelNodes.Dequeue();
                    if(Options.SaveGraph)
                    {
                        RelevantNodes.Add(Current.GetName(), Current);
                    }
                    WriteNodeDetail(Current);
                    //Add the non whitelised "children" of the current node to the queue for future processing
                    if (Current.ImportRejects.Count() > 0)
                    {
                        foreach(var ChildNode in Current.ImportRejects)
                        {
                            CurrentLevelNodes.Enqueue(ChildNode);
                        }
                    }
                }
                CurrentLevel += 1;
                Console.WriteLine();
            }
            //Save the output graph if the user request it
            if(Options.SaveGraph)
            {
                GraphCreator Creater = new GraphCreator(RelevantNodes);
                //Replacing '.' with '_' in the fileName to ensure that the '.' is associated with the file extension
                string FileName = PartName.Replace(".", "_") + ".dgml";
                Creater.SaveGraph(FileName);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Method to display information about a particular node to the user 
        /// </summary>
        /// <param name="Current">The Node whose information we want to display</param>
         
        private void WriteNodeDetail(PartNode Current)
        {
            string Message; 
            if(Current.IsWhiteListed)
            {
                Message = "[Whitelisted] "; 
            } else
            {
                Message = "";
            }
            if (Options.Verbose)
            {
                Message += Current.VerboseMessage;
            }
            else
            {
                Message += GetName(Current.Part, "[Part]");
            }
            Console.WriteLine(Message);
        }

    }

}
