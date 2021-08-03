using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;
using OpenSoftware.DgmlTools.Model;

namespace VSMefx.Commands
{
    class RejectionTracer : Command
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
            while (Errors.Count() > 0)
            {
                //Process all the parts present in the current level of the stack
                var CurrentLevel = Errors.Peek();
                foreach (var Element in CurrentLevel)
                {
                    var Part = Element.Parts.First();
                    //Create a PartNode object from the definition of the current Part
                    ComposablePartDefinition Definition = Part.Definition;
                    string CurrentName = Definition.Type.FullName;
                    if(RejectionGraph.ContainsKey(CurrentName))
                    {
                        RejectionGraph[CurrentName].AddErrorMessage(Element.Message); 
                        continue;
                    }
                    PartNode CurrentNode = new PartNode(Definition, Element.Message, LevelNumber);
                    CurrentNode.SetWhiteListed(this.Creator.IsWhiteListed(CurrentName));
                    //Get the imports for the current part to update the pointers associated with the current node
                    foreach(var Import in Definition.Imports)
                    {
                        string ImportName = Import.ImportingSiteType.FullName;
                        string ImportLabel = "Constructor";
                        if(Import.ImportingMember != null)
                        {
                            ImportLabel = Import.ImportingMember.Name;
                        }
                        if(RejectionGraph.ContainsKey(ImportName))
                        {
                            PartNode ChildNode = RejectionGraph[ImportName];
                            ChildNode.AddParent(CurrentNode, ImportLabel);
                            CurrentNode.AddChild(ChildNode, ImportLabel);
                        }
                    }
                    RejectionGraph.Add(CurrentName, CurrentNode);
                }
                //Get the next level of the stack
                Errors = Errors.Pop();
                LevelNumber -= 1;
            }
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
            if(!Options.Verbose)
            {
                Console.WriteLine();
            }
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
            for(int Level = 1; Level <= MaxLevels; Level++)
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
                        foreach(var ChildEdge in Current.ImportRejects)
                        {
                            CurrentLevelNodes.Enqueue(ChildEdge.Target);
                        }
                    }
                }
                CurrentLevel += 1;
                if(!Options.Verbose)
                {
                    Console.WriteLine();
                }
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
            string StartMessage;
            if (Current.IsWhiteListed)
            {
                StartMessage = "[Whitelisted] ";
            }
            else
            {
                StartMessage = "";
            }
            if (Options.Verbose)
            {
                foreach(string ErrorMessage in Current.VerboseMessages)
                {
                    string Message = StartMessage + ErrorMessage;
                    Console.WriteLine(Message);
                    Console.WriteLine(); 
                }
            }
            else
            {
                string Message = StartMessage + GetName(Current.Part, "[Part]");
                Console.WriteLine(Message);
            }
        }

    }

}
