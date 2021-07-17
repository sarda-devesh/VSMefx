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
        private Dictionary<string, PartNode> rejectionGraph { set; get; }
        private DirectedGraph DGML { get; set; }

        public GraphCreator(Dictionary<string, PartNode> graph)
        {
            this.filterGraph(graph);
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
            var builder = new DgmlBuilder
            {
                NodeBuilders = nodeCreator, 
                LinkBuilders = edgeCreator, 
                CategoryBuilders = categoryCreator
            };
            IEnumerable<PartNode> nodes = rejectionGraph.Values;
            this.DGML = builder.Build(nodes);
        }

        public DirectedGraph getGraph()
        {
            return this.DGML;
        }

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

        private void filterGraph(Dictionary<string,PartNode>  graph)
        {
            this.rejectionGraph = new Dictionary<string, PartNode>(); 
            foreach(var pair in graph)
            {
                if(pair.Value.showNode())
                {
                    this.rejectionGraph.Add(pair.Key, pair.Value); 
                }
            }
            Console.WriteLine("Count of Output Graph is " + this.rejectionGraph.Count()); 
        }

        private Node nodeConverter(PartNode current)
        {
            Node converted = new Node
            {
                Id = current.getName(),
                Category = current.Level.ToString()
            };
            return converted;
        }

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

        private bool validEdge(PartNode source, PartNode target)
        {
            string sourceName = source.getName();
            string targetName = target.getName();
            return (rejectionGraph.ContainsKey(sourceName) && rejectionGraph.ContainsKey(targetName));
        }

    }
}
