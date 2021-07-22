using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenSoftware.DgmlTools;
using OpenSoftware.DgmlTools.Builders;
using OpenSoftware.DgmlTools.Model;

namespace VSMefx.Commands
{
    class GraphCreator
    {
        private Dictionary<string, PartNode> rejectionGraph { set; get; } //The nodes present in the output graph
        private DirectedGraph DGML { get; set; } //The output graph 

        private static readonly string WhiteListProperty = "whitelisted"; 

        public GraphCreator(Dictionary<string, PartNode> graph)
        {
            this.rejectionGraph = graph;
            //Tell the DGML creator how to create nodes, categorize them and create edges between 
            var nodeCreator = new[]
            {
                new NodeBuilder<PartNode>(nodeConverter)
            };
            var edgeCreator = new[]
            {
                new LinksBuilder<PartNode>(edgeGenerator)
            };
            var categoryCreator = new[]
            {
                new CategoryBuilder<PartNode>(x => new Category { Id = x.Level.ToString() } )
            };
            var styleCreator = new[]
            {
                new StyleBuilder<Node>(WhiteListedNode)
            };
            var builder = new DgmlBuilder
            {
                NodeBuilders = nodeCreator, 
                LinkBuilders = edgeCreator, 
                CategoryBuilders = categoryCreator,
                StyleBuilders = styleCreator
            };
            IEnumerable<PartNode> nodes = rejectionGraph.Values;
            this.DGML = builder.Build(nodes);
        }

        public DirectedGraph getGraph()
        {
            return this.DGML;
        }

        /*
         * <summary>
         * Method to save the generated graph to an output file
         * </summary>
         * <param name="outputFileName"> The complete path of the file to which we want to save the DGML graph </param>
         */
        public void saveGraph(string outputFileName)
        {
            int extensionIndex = outputFileName.LastIndexOf('.');
            string extension = outputFileName.Substring(extensionIndex + 1);
            if(!extension.Equals("dgml"))
            {
                Console.WriteLine("Can't save graph to ouput file " + outputFileName);
                return;
            } 
            this.DGML.WriteToFile(outputFileName);
            Console.WriteLine("Saved rejection graph to " + outputFileName);
        }

        /*
         * <summary>
         * Method to convert from custom Node representation to the DGML node representation
         * <summary>
         * <param name="current">The PartNode object which we want to convert</param>
         * <returns> A DGML Node representation of the input PartNode </returns>
         */
        private Node nodeConverter(PartNode current)
        {
            string property; 
            if(current.IsWhiteListed)
            {
                property = WhiteListProperty;
            } else
            {
                property = "Error"; 
            }
            Node converted = new Node
            {
                Id = current.getName(),
                Category = property
            };
            converted.Properties.Add("Level", current.Level.ToString());
            return converted;
        }

        /*
         * <summary>
         * Method to get all the outgoing edges from the current node
         * </summary>
         * <param name="current">The PartNode whose outgoing edges we want to find </param>
         * <returns> A list of Links that represent the outgoing edges for the input node </returns>
         */
        private IEnumerable<Link> edgeGenerator(PartNode current)
        {
            foreach(var parentNode in current.rejectsCaused)
            {
                if(validEdge(current, parentNode))
                {
                    Link edge = new Link
                    {
                        Source = current.getName(),
                        Target = parentNode.getName()
                    };
                    yield return edge; 
                } 
            }
        }

        /*
         * <summary>
         * Method to check if a given potential edge is valid or not
         * </summary>
         * <param name="Source">The PartNode that would be the source of the potential edge </param>
         * <param name="Target">The PartNode that would be the destination of the potential edge </param>
         * <returns> A boolean indicating if the specified edge should be included in the graph or not </returns>
         */

        private bool validEdge(PartNode source, PartNode target)
        {
            string sourceName = source.getName();
            string targetName = target.getName();
            return (rejectionGraph.ContainsKey(sourceName) && rejectionGraph.ContainsKey(targetName));
        }

        private static Style WhiteListedNode(Node node)
        {
            return new Style
            {
                GroupLabel = WhiteListProperty,
                Setter = new List<Setter>
                {
                    new Setter {Property = "Background", Value = "#FFFFFF" }
                }
            };
        }

    }
}
