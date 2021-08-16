using Microsoft.VisualStudio.Composition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSMefx.Commands
{
    class PartNode
    {
        public ComposablePartDefinition Part { get; set; } //Represents the part associated with the current node
        public List<string> VerboseMessages { get; set; } //Rejection Message(s) associated with the current part
        
         /// <summary>
         /// Stores the "children" of the current node, which represents parts that the current
         /// node imports that have import issues themselves
         /// </summary>
        public HashSet<PartEdge> ImportRejects;

        /// <summary>
        /// Stores the "parent" of the current node, which represents parts that the current
        /// node caused import issues in due to its failure 
        /// </summary>
        public HashSet<PartEdge> RejectsCaused; 
        public int Level { get; private set; } //An indicator of its depth in the rejection stack 

        public bool IsWhiteListed { get; private set;  } //A boolean if this part was specified in the whitelist file

        public List<string> ExportingContracts { get; private set; }

        public PartNode(ComposablePartDefinition Definition, string Message, int CurrLevel)
        {
            this.Part = Definition;
            this.IsWhiteListed = Part.Metadata.ContainsKey("RejectionExpectex"); 
            this.VerboseMessages = new List<string> { Message };
            ImportRejects = new HashSet<PartEdge>();
            RejectsCaused = new HashSet<PartEdge>();
            this.Level = CurrLevel;
            this.IsWhiteListed = false;

            this.ExportingContracts = new List<string>();
            var NodeName = this.GetName();
            foreach (var Export in Part.ExportDefinitions)
            {
                var ContractName = Export.Value.ContractName;
                if(!ContractName.Equals(NodeName))
                {
                    ExportingContracts.Add(ContractName);
                }
            }
        }

        public string GetName()
        {
            return this.Part.Type.FullName;
        }
        
        /// <summary>
        /// Method to check if the given node imports any parts with import issues
        /// </summary>
        /// <returns> A boolean indicating it the given node is a leaf node</returns>
        public bool IsLeafNode()
        {
            return ImportRejects.Count() == 0;
        }

        public void AddChild(PartNode Node, string Description = "")
        {
            ImportRejects.Add(new PartEdge(Node, Description));
        }

        public void AddParent(PartNode Node, string Description = "")
        {
            RejectsCaused.Add(new PartEdge(Node, Description));
        }

        public void SetWhiteListed(bool Value)
        {
            this.IsWhiteListed = Value; 
        }

        public void AddErrorMessage(string Message)
        {
            this.VerboseMessages.Add(Message); 
        }

        public bool HasExports()
        {
            return this.ExportingContracts.Count() > 0;
        }

    }

    /// <summary>
    /// A class to represent a simple edge between nodes
    /// </summary>
    class PartEdge
    {
        public PartNode Target; //The node that is at the head of the directed edge
        public string Label; //A label to display when drawing the directed edge 

        public PartEdge(PartNode Other, string Description)
        {
            this.Target = Other;
            this.Label = Description;
        }
    }
}
