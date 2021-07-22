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
        public ComposablePartDefinition part { get; set; } //Represents the part associated with the current node
        public string verboseMessage { get; set; } //Rejection Message(s) associated with the current part

        /*
         * <summary>
         * Stores the "children" of the current node, which represents parts that the current
         * node imports that have import issues themselves
         * </summary>
         */
        public HashSet<PartNode> importRejects;

        /*
         * <summary>
         * Stores the "parent" of the current node, which represents parts that the current
         * node caused import issues in due to its failure 
         * </summary>
         */

        public HashSet<PartNode> rejectsCaused; 
        public int Level { get; private set; } //An indicator of its depth in the rejection stack 

        public bool IsWhiteListed { get; private set;  } //A boolean if this part was specified in the whitelist file

        public PartNode(ComposablePartDefinition definition, string message, int currLevel)
        {
            this.part = definition;
            this.verboseMessage = message;
            importRejects = new HashSet<PartNode>();
            rejectsCaused = new HashSet<PartNode>();
            this.Level = currLevel;
            this.IsWhiteListed = false; 
        }

        public string getName()
        {
            return part.Type.FullName;
        }

        /*
         * <summary>
         * Method to check if the given node imports any parts with import issues
         * </summary>
         * <returns> A boolean indicating it the given node is a leaf node</return>
         */
        public bool isLeafNode()
        {
            return importRejects.Count() == 0;
        }

        public void addChild(PartNode node)
        {
            importRejects.Add(node);
        }

        public void addParent(PartNode node)
        {
            rejectsCaused.Add(node);
        }

        public void setWhiteListed(bool value)
        {
            this.IsWhiteListed = value; 
        }

    }
}
