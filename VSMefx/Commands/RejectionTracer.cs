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
        /*
         * <summary>
         * All the nodes in the rejectionGraph, which is a graph representation of 
         * the error stack provided by the config 
         * </summary>
         */
        private Dictionary<string, PartNode> rejectionGraph { get; set; } 

        /*
         * <summary>
         * The number of levels present in the overall graph where the level value
         * corresponds to the depth of the node/part in the error stack
         * </summary>
         */
        private int maxLevels { get; set; } 

        /*
         * <summary>
         * Method to initialize the part nodes and thier "pointers" based on the error
         * stack from the config
         * </summary>
         */
        private void generateNodeGraph()
        {
            //Get the error stack from the composition configuration
            CompositionConfiguration config = this.Creator.config;
            var errors = config.CompositionErrors;
            int levelNumber = errors.Count();
            this.maxLevels = levelNumber;
            while (errors.Count() > 0)
            {
                //Process all the parts present in the current level of the stack
                var currentLevel = errors.Peek();
                foreach (var element in currentLevel)
                {
                    var parts = element.Parts;
                    foreach (var part in parts)
                    {
                        //Create a PartNode object from the definition of the current Part
                        ComposablePartDefinition definition = part.Definition;
                        PartNode currentNode = new PartNode(definition, element.Message, levelNumber);
                        string currentName = currentNode.getName();
                        currentNode.setWhiteListed(this.Creator.isWhiteListed(currentName));
                        //Get the imports for the current part to update the pointers associated with the current node
                        var imports = part.Definition.Imports;
                        foreach(var import in imports)
                        { 
                            string importName = import.ImportDefinition.ContractName;
                            /*
                             * As stated in the documentation for CompositionConfiguration, errors near the top of the stack
                             * cause errors in elements near the point of the stack. Thus, we check the graph for the import
                             * since any import that has rejection issues would have already been processed. 
                             */
                            if (rejectionGraph.ContainsKey(importName))
                            {
                                PartNode childNode = rejectionGraph[importName];
                                childNode.addParent(currentNode); //Make the current node a "parent" of the import Node
                                currentNode.addChild(childNode); //Make the import node a "child" of the current Node
                            } 
                        }
                        //If we have already processed the current part before - potential error with the rejection stack
                        if(rejectionGraph.ContainsKey(currentName))
                        {
                            throw new Exception("Node already present in graph, potential issue with duplicates");
                        }
                        rejectionGraph.Add(currentName, currentNode);
                    }
                }
                //Get the next level of the stack
                errors = errors.Pop();
                levelNumber -= 1;
            }
        }

        public RejectionTracer(ConfigCreator DerivedInfo, CLIOptions Arguments) : base(DerivedInfo, Arguments)
        {
            this.rejectionGraph = new Dictionary<string, PartNode>();
            this.generateNodeGraph();
        }

        /*
         * <summary>
         * Method to indicate all the rejection issues present in a given level
         * </summary>
         * <param name="currentLevel">An integer representing the level we are intrested in</param>
         */
        private void listErrorsinLevel(int currentLevel)
        {
            Console.WriteLine("Listing errors in level " + currentLevel);
            foreach(var pair in this.rejectionGraph)
            {
                PartNode node = pair.Value;
                if(node.Level.Equals(currentLevel))
                {
                    writeNodeDetail(node);
                    
                }
            }
            Console.WriteLine();
        }

        /*
         * <summary>
         * Method to display all the rejections present in the input files.
         * Based on how the levels are assigned, the root causes for the errors in the 
         * application can easily be accessed by looking at the rejection issues present at
         * the highest level. 
         * If the graph argument was passed in the input arguments, a DGML graph representing
         * all the rejection issues is saved in a file called all.dgml
         * </summary>
         */
        public void listAllRejections ()
        {
            for(int level = maxLevels; level > 0; level--)
            {
                listErrorsinLevel(level);
            }
            if(Options.saveGraph)
            {
                GraphCreator creater = new GraphCreator(rejectionGraph);
                creater.saveGraph("all.dgml");
            }
        }

        /* 
         * <summary>
         * Method to the get the information about the rejection information that caused a
         * particular import failure, rather than for the entire system.
         * Once again, the root causes can easily be accessed by looking at the rejection
         * issues at the highest levels of the output. 
         * If graph was specified in the input arguments then a DGML graph tracing the rejection
         * chain assocaited with the current path alone is saved to a file called [partName].dgml
         * <param name = "partName"> The name of the part which we want to analyze</param>
         * </summary>
        */
        public void listReject(string partName)
        {
            //Deal with the case that there are no rejection issues with the given part
            if (!rejectionGraph.ContainsKey(partName))
            {
                Console.WriteLine("No Rejection Issues associated with " + partName + "\n");
                return;
            }
            Console.WriteLine("Printing Rejection Graph Info for " + partName + "\n");
            //Store just the nodes that are involved in the current rejection chain to use when generating the graph
            Dictionary<string, PartNode> relevantNodes = null; 
            if (Options.saveGraph)
            {
                relevantNodes = new Dictionary<string, PartNode>();
            }
            /*
             * Perform Breadth First Search (BFS) with the node associated with partName as the root. 
             * When performing BFS, only the child nodes are considered since we want to the root to be
             * the end point of the rejection chain(s). 
             * BFS was chosen over DFS because of the fact that we process level by level when performing
             * the travesal and thus easier to communicate the causes and pathway to the end user
             */
            Queue<PartNode> currentLevelNodes = new Queue<PartNode>();
            currentLevelNodes.Enqueue(rejectionGraph[partName]);
            int currentLevel = 1; 
            while(currentLevelNodes.Count() > 0)
            {
                Console.WriteLine("Errors in Level " + currentLevel);
                //Iterate through all the nodes in the current level
                int numNodes = currentLevelNodes.Count();
                for (int i = 0; i < numNodes; i++)
                {
                    //Process the current node by displaying its import issue and adding it to the graph
                    PartNode current = currentLevelNodes.Dequeue();
                    if(Options.saveGraph)
                    {
                        relevantNodes.Add(current.getName(), current);
                    }
                    writeNodeDetail(current);
                    //Add the non whitelised "children" of the current node to the queue for future processing
                    if (current.importRejects.Count() > 0)
                    {
                        foreach(var node in current.importRejects)
                        {
                            currentLevelNodes.Enqueue(node);
                        }
                    }
                }
                currentLevel += 1;
                Console.WriteLine();
            }
            //Save the output graph if the user request it
            if(Options.saveGraph)
            {
                GraphCreator creater = new GraphCreator(relevantNodes);
                //Replacing '.' with '_' in the fileName to ensure that the '.' is associated with the file extension
                string fileName = partName.Replace(".", "_") + ".dgml";
                creater.saveGraph(fileName);
                Console.WriteLine();
            }
        }

        /*
         * <summary>
         * Method to display information about a particular node to the user 
         * </summary>
         */
        private void writeNodeDetail(PartNode current)
        {
            string message; 
            if(current.IsWhiteListed)
            {
                message = "[Whitelisted] "; 
            } else
            {
                message = "";
            }
            if (Options.verbose)
            {
                message += current.verboseMessage;
            }
            else
            {
                message += getName(current.part, "[Part]");
            }
            Console.WriteLine(message);
        }

    }

}
