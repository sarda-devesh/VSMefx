using Microsoft.VisualStudio.Composition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSMefx.Commands
{
    public class PartNode
    {
        public ComposablePartDefinition Part { get; set; } //Represents the part associated with the current node
        public string VerboseMessage { get; set; } //Rejection Message(s) associated with the current part

        /*
         * <summary>
         * Stores the "children" of the current node, which represents parts that the current
         * node imports that have import issues themselves
         * </summary>
         */
        public HashSet<PartNode> ImportRejects;

        /*
         * <summary>
         * Stores the "parent" of the current node, which represents parts that the current
         * node caused import issues in due to its failure 
         * </summary>
         */

        public HashSet<PartNode> RejectsCaused; 
        public int Level { get; private set; } //An indicator of its depth in the rejection stack 

        public bool IsWhiteListed { get; private set;  } //A boolean if this part was specified in the whitelist file

        public PartNode(ComposablePartDefinition Definition, string Message, int CurrLevel)
        {
            this.Part = Definition;
            this.VerboseMessage = Message;
            ImportRejects = new HashSet<PartNode>();
            RejectsCaused = new HashSet<PartNode>();
            this.Level = CurrLevel;
            this.IsWhiteListed = false; 
        }

        public string GetName()
        {
            return Part.Type.FullName;
        }

        
        /// <summary>
        /// Method to check if the given node imports any parts with import issues
        /// </summary>
        /// <returns> A boolean indicating it the given node is a leaf node</return>
        
        public bool IsLeafNode()
        {
            return ImportRejects.Count() == 0;
        }

        public void AddChild(PartNode Node)
        {
            ImportRejects.Add(Node);
        }

        public void AddParent(PartNode Node)
        {
            RejectsCaused.Add(Node);
        }

        public void SetWhiteListed(bool Value)
        {
            this.IsWhiteListed = Value; 
        }

    }
}
