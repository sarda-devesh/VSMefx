namespace VSMefx.Commands
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.Composition;

    /// <summary>
    /// Class to perform rejection tracing on the input parts.
    /// </summary>
    internal class RejectionTracer : Command
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RejectionTracer"/> class.
        /// </summary>
        /// <param name="derivedInfo">The catalog and config associated with the input files.</param>
        /// <param name="arguments">The arguments specified by the user.</param>
        public RejectionTracer(ConfigCreator derivedInfo, CLIOptions arguments)
            : base(derivedInfo, arguments)
        {
            this.RejectionGraph = new Dictionary<string, PartNode>();
            this.GenerateNodeGraph();
        }

        /// <summary>
        /// Gets or sets all the nodes in the rejectionGraph, which is a graph representation of
        /// the error stack provided by the config.
        /// </summary>
        private Dictionary<string, PartNode> RejectionGraph { get; set; }

        /// <summary>
        /// Gets or sets the number of levels present in the overall graph where the level value
        /// corresponds to the depth of the node/part in the error stack.
        /// </summary>
        private int MaxLevels { get; set; }

        /// <summary>
        /// Method to read the input arguments and perform rejection tracing for the requested parts.
        /// </summary>
        public void PerformRejectionTracing()
        {
            if (this.Options.RejectedDetails.Contains("all") ||
                this.Options.RejectedDetails.Contains("All"))
            {
                this.ListAllRejections();
            }
            else
            {
                foreach (string rejectPart in this.Options.RejectedDetails)
                {
                    this.ListReject(rejectPart);
                }
            }
        }

        /// <summary>
        /// Method to initialize the part nodes and thier "pointers" based on the error
        /// stack from the config.
        /// </summary>
        private void GenerateNodeGraph()
        {
            // Get the error stack from the composition configuration
            CompositionConfiguration config = this.Creator.Config;
            var errors = config.CompositionErrors;
            int levelNumber = 1;
            while (errors.Count() > 0)
            {
                // Process all the parts present in the current level of the stack
                var currentLevel = errors.Peek();
                foreach (var element in currentLevel)
                {
                    var part = element.Parts.First();

                    // Create a PartNode object from the definition of the current Part
                    ComposablePartDefinition definition = part.Definition;
                    string currentName = definition.Type.FullName;
                    if (currentName == null)
                    {
                        continue;
                    }

                    if (this.RejectionGraph.ContainsKey(currentName))
                    {
                        this.RejectionGraph[currentName].AddErrorMessage(element.Message);
                        continue;
                    }

                    PartNode currentNode = new PartNode(definition, element.Message, levelNumber);
                    currentNode.SetWhiteListed(this.Creator.IsWhiteListed(currentName));
                    this.RejectionGraph.Add(currentName, currentNode);
                }

                // Get the next level of the stack
                errors = errors.Pop();
                levelNumber += 1;
            }

            this.MaxLevels = levelNumber - 1;
            foreach (var nodePair in this.RejectionGraph)
            {
                var node = nodePair.Value;
                var currentNodeName = node.GetName();
                var nodeDefinition = node.Part;

                // Get the imports for the current part to update the pointers associated with the current node
                foreach (var import in nodeDefinition.Imports)
                {
                    string importName = import.ImportingSiteType.FullName;
                    if (importName == null || !this.RejectionGraph.ContainsKey(importName))
                    {
                        continue;
                    }

                    string importLabel = importName;
                    if (import.ImportingMember != null)
                    {
                        importLabel = import.ImportingMember.Name;
                    }

                    PartNode childNode = this.RejectionGraph[importName];
                    childNode.AddParent(node, importLabel);
                    node.AddChild(childNode, importLabel);
                }
            }
        }

        /// <summary>
        /// Method to indicate all the rejection issues present in a given level.
        /// </summary>
        /// <param name="currentLevel">An integer representing the level we are intrested in.</param>
        private void ListErrorsinLevel(int currentLevel)
        {
            Console.WriteLine("Errors in level " + currentLevel);
            foreach (var pair in this.RejectionGraph)
            {
                PartNode currentNode = pair.Value;
                if (currentNode.Level.Equals(currentLevel))
                {
                    this.WriteNodeDetail(currentNode);
                }
            }

            if (!this.Options.Verbose)
            {
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Method to get path to save the dgml file in.
        /// </summary>
        /// <param name="fileName">Name of the dgml file whose path we want to determine.</param>
        /// <returns>The complete absolute path to save the dgml file in.</returns>
        private string GetGraphPath(string fileName)
        {
            string relativePath = this.Options.GraphPath;
            string currentDirectory = Directory.GetCurrentDirectory();
            string outputDirectory = Path.GetFullPath(Path.Combine(currentDirectory, relativePath));
            if (!Directory.Exists(outputDirectory))
            {
                string missingMessage = "Couldn't find directory " + relativePath + " so saving rejection graph to current directory";
                Console.WriteLine(missingMessage);
                outputDirectory = currentDirectory;
            }

            string outputPath = Path.GetFullPath(Path.Combine(outputDirectory, fileName));
            return outputPath;
        }

        /// <summary>
        /// Method to display all the rejections present in the input files.
        /// If the graph argument was passed in the input arguments, a DGML graph representing
        /// all the rejection issues is saved in a file called All.dgml.
        /// </summary>
        /// <remarks>
        /// Based on how the levels are assigned, the root causes for the errors in the
        /// application can easily be accessed by looking at the rejection issues present at
        /// the highest level.
        /// </remarks>
        private void ListAllRejections()
        {
            Console.WriteLine("Listing all the rejection issues");
            for (int level = this.MaxLevels; level > 0; level--)
            {
                this.ListErrorsinLevel(level);
            }

            bool saveGraph = this.Options.GraphPath.Length > 0;
            if (saveGraph)
            {
                GraphCreator creater = new GraphCreator(this.RejectionGraph);
                creater.SaveGraph(GetGraphPath("AllErrors.dgml"));
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Method to the get the information about the rejection information that caused a
        /// particular import failure, rather than for the entire system.
        /// If graph was specified in the input arguments then a DGML graph tracing the rejection
        /// chain assocaited with the current path alone is saved to a file called [partName].dgml.
        /// </summary>
        /// <param name = "partName"> The name of the part which we want to analyze.</param>
        /// <remarks>
        /// Once again, the root causes can easily be accessed by looking at the rejection
        /// issues at the highest levels of the output.
        /// </remarks>
        private void ListReject(string partName)
        {
            // Deal with the case that there are no rejection issues with the given part
            if (!this.RejectionGraph.ContainsKey(partName))
            {
                Console.WriteLine("No Rejection Issues associated with " + partName + "\n");
                return;
            }

            Console.WriteLine("Printing Rejection Graph Info for " + partName + "\n");

            // Store just the nodes that are involved in the current rejection chain to use when generating the graph
            Dictionary<string, PartNode> relevantNodes = null;
            bool saveGraph = this.Options.GraphPath.Length > 0;
            if (saveGraph)
            {
                relevantNodes = new Dictionary<string, PartNode>();
            }

            // Perform Breadth First Search (BFS) with the node associated with partName as the root.
            // When performing BFS, only the child nodes are considered since we want to the root to be
            // the end point of the rejection chain(s).
            // BFS was chosen over DFS because of the fact that we process level by level when performing
            // the travesal and thus easier to communicate the causes and pathway to the end user
            Queue<PartNode> currentLevelNodes = new Queue<PartNode>();
            currentLevelNodes.Enqueue(this.RejectionGraph[partName]);
            while (currentLevelNodes.Count() > 0)
            {
                int currentLevel = currentLevelNodes.Peek().Level;
                Console.WriteLine("Errors in Level " + currentLevel);

                // Iterate through all the nodes in the current level
                int numNodes = currentLevelNodes.Count();
                for (int index = 0; index < numNodes; index++)
                {
                    // Process the current node by displaying its import issue and adding it to the graph
                    PartNode current = currentLevelNodes.Dequeue();
                    if (saveGraph)
                    {
                        relevantNodes.Add(current.GetName(), current);
                    }

                    this.WriteNodeDetail(current);

                    // Add the non whitelised "children" of the current node to the queue for future processing
                    if (current.ImportRejects.Count() > 0)
                    {
                        foreach (var childEdge in current.ImportRejects)
                        {
                            currentLevelNodes.Enqueue(childEdge.Target);
                        }
                    }
                }

                if (!this.Options.Verbose)
                {
                    Console.WriteLine();
                }
            }

            // Save the output graph if the user request it
            if (saveGraph)
            {
                GraphCreator creater = new GraphCreator(relevantNodes);

                // Replacing '.' with '_' in the fileName to ensure that the '.' is associated with the file extension
                string fileName = partName.Replace(".", "_") + ".dgml";
                creater.SaveGraph(GetGraphPath(fileName));
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Method to display information about a particular node to the user.
        /// </summary>
        /// <param name="current">The Node whose information we want to display.</param>
        private void WriteNodeDetail(PartNode current)
        {
            string startMessage;
            if (current.IsWhiteListed)
            {
                startMessage = "[Whitelisted] ";
            }
            else
            {
                startMessage = string.Empty;
            }

            if (this.Options.Verbose)
            {
                foreach (string errorMessage in current.VerboseMessages)
                {
                    string message = startMessage + errorMessage;
                    Console.WriteLine(message);
                    Console.WriteLine();
                }
            }
            else
            {
                string message = startMessage + this.GetName(current.Part, "[Part]");
                Console.WriteLine(message);
            }
        }
    }
}
