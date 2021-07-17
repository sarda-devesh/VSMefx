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
        public ComposablePartDefinition part { get; set; }
        public string verboseMessage { get; set; }
        public HashSet<PartNode> importRejects; //Points to the "children" of the current node
        public HashSet<PartNode> rejectsCaused; //Points to the "parents" of the current node
        public int Level { get; private set; }

        public bool IsWhiteListed { get; private set;  }

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

        public bool showNode()
        {
            return (!this.IsWhiteListed || this.importRejects.Count() > 0); 
        }

    }
}
