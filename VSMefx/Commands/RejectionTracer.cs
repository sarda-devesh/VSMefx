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

        private Dictionary<string, PartNode> rejectionGraph { get; set; }
        private int maxLevels { get; set; }

        private void generateNodeGraph()
        {
            CompositionConfiguration config = this.Creator.config;
            var errors = config.CompositionErrors;
            int levelNumber = errors.Count();
            this.maxLevels = levelNumber;
            HashSet<String> whiteListedParts = this.Creator.WhiteListedParts; 
            while (errors.Count() > 0)
            {
                var currentLevel = errors.Peek();
                foreach (var element in currentLevel)
                {
                    var parts = element.Parts;
                    foreach (var part in parts)
                    {
                        ComposablePartDefinition definition = part.Definition;
                        PartNode currentNode = new PartNode(definition, element.Message, levelNumber);
                        string currentName = currentNode.getName();
                        if (whiteListedParts.Contains(currentName)) {
                            currentNode.setWhiteListed(true); 
                        }
                        var imports = part.Definition.Imports;
                        foreach(var import in imports)
                        { 
                            string importName = import.ImportDefinition.ContractName;
                            if(rejectionGraph.ContainsKey(importName))
                            {
                                PartNode childNode = rejectionGraph[importName];
                                if(!childNode.IsWhiteListed)
                                {
                                    childNode.addParent(currentNode);
                                    currentNode.addChild(childNode);
                                }
                            } 
                        }
                        if(rejectionGraph.ContainsKey(currentName))
                        {
                            throw new Exception("Node already present in graph, potential issue with duplicates");
                        }
                        rejectionGraph.Add(currentName, currentNode);
                    }
                }
                errors = errors.Pop();
                levelNumber -= 1;
            }
        }

        public RejectionTracer(ConfigCreator DerivedInfo, CLIOptions Arguments) : base(DerivedInfo, Arguments)
        {
            this.rejectionGraph = new Dictionary<string, PartNode>();
            this.generateNodeGraph();
        }

        private void listErrorsinLevel(int currentLevel)
        {
            Console.WriteLine("Listing errors in level " + currentLevel);
            HashSet<String> whiteList = this.Creator.WhiteListedParts; 
            foreach(var pair in this.rejectionGraph)
            {
                PartNode node = pair.Value;
                if(!node.IsWhiteListed && node.Level.Equals(currentLevel))
                {
                    writeNodeDetail(node);
                    
                }
            }
            Console.WriteLine();
        }

        public void listAllRejections ()
        {
            for(int level = 1; level <= maxLevels; level++)
            {
                listErrorsinLevel(level);
            }
            if(Options.saveGraph)
            {
                GraphCreator creater = new GraphCreator(rejectionGraph);
                creater.saveGraph("all.dgml");
            }
        }

        /* Performs BFS on the rejection Graph using the input part as the root of the graph
        *  Thus errors in lower levels are caused by errors in the higher levels or parts in level 1
        *  import parts in level 2. Thus to analyze the root causes, look at the parts in the highest
        *  level which correspond to the leaf nodes
        */
        public void listReject(string partName)
        {
            if (!rejectionGraph.ContainsKey(partName))
            {
                Console.WriteLine("No Rejection Issues associated with " + partName + "\n");
                return;
            }
            HashSet<string> whiteList = this.Creator.WhiteListedParts;
            if (whiteList.Contains(partName))
            {
                Console.WriteLine("Not running rejection analysis since part " + partName + " is present in whitelist\n"); 
                return; 
            }
            Console.WriteLine("Printing Rejection Graph Info for " + partName + "\n");
            //Check if the partName was part of the whiteList
            
            Dictionary<string, PartNode> relevantNodes = null;
            if (Options.saveGraph)
            {
                relevantNodes = new Dictionary<string, PartNode>();
            }
            Queue<PartNode> currentLevelNodes = new Queue<PartNode>();
            currentLevelNodes.Enqueue(rejectionGraph[partName]);
            int currentLevel = 1; 
            while(currentLevelNodes.Count() > 0)
            {
                Console.WriteLine("Errors in Level " + currentLevel);
                int numNodes = currentLevelNodes.Count();
                for (int i = 0; i < numNodes; i++)
                {
                    PartNode current = currentLevelNodes.Dequeue();
                    if(Options.saveGraph)
                    {
                        relevantNodes.Add(current.getName(), current);
                    }
                    writeNodeDetail(current);
                    if (current.importRejects.Count() > 0)
                    {
                        foreach(var node in current.importRejects)
                        {
                            if (!node.IsWhiteListed)
                            {
                                currentLevelNodes.Enqueue(node);
                            }
                        }
                    }
                }
                currentLevel += 1;
                Console.WriteLine();
            }
            if(Options.saveGraph)
            {
                GraphCreator creater = new GraphCreator(relevantNodes);
                string fileName = partName.Replace(".", "_") + ".dgml";
                creater.saveGraph(fileName);
                Console.WriteLine();
            }
        }

        private void writeNodeDetail(PartNode current)
        {
            if (Options.verbose)
            {
                Console.WriteLine(current.verboseMessage);
            }
            else
            {
                Console.WriteLine(getName(current.part, "[Part]"));
            }
        }

    }

}
