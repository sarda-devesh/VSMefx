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
        private Dictionary<string, PartNode> RejectionGraph { set; get; } //The nodes present in the output graph
        private DirectedGraph Dgml { get; set; } //The output graph 

        private static readonly string WhiteListLabel = "Whitelisted";
        private static readonly string EdgeLabel = "Edge";
        private static readonly string NodeBackgroundHex = "#FFFFFF";
        private static readonly string EdgeColor = "#00FFFF";
        private static readonly string EdgeThickness = "3";

        public GraphCreator(Dictionary<string, PartNode> Graph)
        {
            this.RejectionGraph = Graph;
            //Tell the DGML creator how to create nodes, categorize them and create edges between 
            var NodeCreator = new[]
            {
                new NodeBuilder<PartNode>(NodeConverter)
            };
            var EdgeCreator = new[]
            {
                new LinksBuilder<PartNode>(EdgeGenerator)
            };
            var CategoryCreator = new[]
            {
                new CategoryBuilder<PartNode>(x => new Category { Id = x.Level.ToString() } )
            };
            StyleBuilder[] StyleCreator =  {
                new StyleBuilder<Node>(WhiteListedNode),
                new StyleBuilder<Link>(EdgeStyle)
            };
            var builder = new DgmlBuilder
            {
                NodeBuilders = NodeCreator, 
                LinkBuilders = EdgeCreator, 
                CategoryBuilders = CategoryCreator,
                StyleBuilders = StyleCreator
            };
            IEnumerable<PartNode> nodes = RejectionGraph.Values;
            this.Dgml = builder.Build(nodes);
        }

        public DirectedGraph GetGraph()
        {
            return this.Dgml;
        }

        /// <summary>
        /// Method to save the generated graph to an output file
        /// </summary>
        /// <param name="OutputFileName"> The complete path of the file to which we want to save the DGML graph </param>

        public void SaveGraph(string OutputFileName)
        {
            int extensionIndex = OutputFileName.LastIndexOf('.');
            string extension = OutputFileName.Substring(extensionIndex + 1);
            if(!extension.Equals("dgml"))
            {
                Console.WriteLine("Can't save graph to ouput file " + OutputFileName);
                return;
            } 
            this.Dgml.WriteToFile(OutputFileName);
            Console.WriteLine("Saved rejection graph to " + OutputFileName);
        }

        /// <summary>
        /// Method to convert from custom Node representation to the DGML node representation
        /// <summary>
        /// <param name="Current">The PartNode object which we want to convert</param>
        /// <returns> A DGML Node representation of the input PartNode </returns>
        
        private Node NodeConverter(PartNode Current)
        {
            string Property; 
            if(Current.IsWhiteListed)
            {
                Property = WhiteListLabel;
            } else
            {
                Property = "Error"; 
            }
            Node Convertered = new Node
            {
                Id = Current.GetName(),
                Category = Property
            };
            Convertered.Properties.Add("Level", Current.Level.ToString());
            return Convertered;
        }

        /// <summary>
        /// Method to get all the outgoing edges from the current node
        /// </summary>
        /// <param name="Current">The PartNode whose outgoing edges we want to find </param>
        /// <returns> A list of Links that represent the outgoing edges for the input node </returns>
        private IEnumerable<Link> EdgeGenerator(PartNode Current)
        {
            foreach(var OutgoingEdge in Current.RejectsCaused)
            {
                if(ValidEdge(Current, OutgoingEdge))
                {
                    Link Edge = new Link
                    {
                        Source = Current.GetName(),
                        Target = OutgoingEdge.Target.GetName(),
                        Label = OutgoingEdge.Label,
                        Category = EdgeLabel
                    };
                    yield return Edge; 
                } 
            }
        }

        /// <summary>
        /// Method to check if a given potential edge is valid or not
        /// </summary>
        /// <param name="Source">The PartNode that would be the source of the potential edge </param>
        /// <param name="Edge">The PartEdge indicating an outgoing edge from the Source Node</param>
        /// <returns> A boolean indicating if the specified edge should be included in the graph or not </returns>
        private bool ValidEdge(PartNode Source, PartEdge Edge)
        {
            string sourceName = Source.GetName();
            string targetName = Edge.Target.GetName();
            return (RejectionGraph.ContainsKey(sourceName) && RejectionGraph.ContainsKey(targetName));
        }

        /// <summary>
        /// Returns a Style object that sets the background of whitelisted nodes to white
        /// </summary>
        /// <param name="Node"></param>
        /// <returns>The Style object for a whitelisted node</returns>
        private static Style WhiteListedNode(Node Node)
        {
            return new Style
            {
                GroupLabel = WhiteListLabel,
                Setter = new List<Setter>
                {
                    new Setter {Property = "Background", Value = NodeBackgroundHex }
                }
            };
        }

        private static Style EdgeStyle(Link Edge)
        {
            return new Style
            {
                GroupLabel = EdgeLabel,
                Setter = new List<Setter>
                {
                    new Setter { Property = "Background", Value = EdgeColor },
                    new Setter {Property = "StrokeThickness", Value = EdgeThickness}
                }
            };
        }

    }
}
