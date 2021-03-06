namespace VSMefx.Commands
{
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Composition;

    /// <summary>
    /// Node object to represent parts when performing rejection tracing.
    /// </summary>
    internal class PartNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartNode"/> class.
        /// </summary>
        /// <param name="definition">The definition of the part associated with the node.</param>
        /// <param name="message">Initialize rejection message in error stack.</param>
        /// <param name="currLevel">The depth of the part in the stack.</param>
        public PartNode(ComposablePartDefinition definition, string message, int currLevel)
        {
            this.Part = definition;
            this.IsWhiteListed = this.Part.Metadata.ContainsKey("RejectionExpectex");
            this.VerboseMessages = new HashSet<string> { message };
            this.ImportRejects = new HashSet<PartEdge>();
            this.RejectsCaused = new HashSet<PartEdge>();
            this.Level = currLevel;
            this.IsWhiteListed = false;

            this.ExportingContracts = new List<string>();
            foreach (var export in this.Part.ExportDefinitions)
            {
                if (export.Key == null)
                {
                    continue;
                }

                this.ExportingContracts.Add(export.Value.ContractName);
            }
        }

        /// <summary>
        /// Gets the definition associated with the current part.
        /// </summary>
        public ComposablePartDefinition Part { get; private set; } // Represents the part associated with the current node

        /// <summary>
        /// Gets the verbose rejection messages associated with the current part.
        /// </summary>
        public HashSet<string> VerboseMessages { get; private set; } // Rejection Message(s) associated with the current part

         /// <summary>
         /// Gets the "children" of the current node, which represents parts that the current
         /// node imports that have import issues themselves.
         /// </summary>
        public HashSet<PartEdge> ImportRejects { get; private set; }

        /// <summary>
        /// Gets the "parent" of the current node, which represents parts that the current
        /// node caused import issues in due to its failure.
        /// </summary>
        public HashSet<PartEdge> RejectsCaused { get; private set; }

        /// <summary>
        /// Gets the level of the current node, which serves as an indicator
        /// of its depth in the rejection stack.
        /// </summary>
        public int Level { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current node has been whitelisted by the user.
        /// </summary>
        public bool IsWhiteListed { get; private set;  }

        /// <summary>
        /// Gets the name of contracts exported by the part other than itself.
        /// </summary>
        public List<string> ExportingContracts { get; private set; }

        /// <summary>
        /// Method to get the label for the node.
        /// </summary>
        /// <returns>The name of the associated part.</returns>
        public string GetName()
        {
            return this.Part.Type.FullName;
        }

        /// <summary>
        /// Method to check if the given node imports any parts with import issues.
        /// </summary>
        /// <returns> A boolean indicating it the given node is a leaf node.</returns>
        public bool IsLeafNode()
        {
            return this.ImportRejects.Count == 0;
        }

        /// <summary>
        /// Method to add a node that caused a rejection error in the current node.
        /// </summary>
        /// <param name="node">The node that is the cause of the error.</param>
        /// <param name="description">Label to use when visualizing the edge.</param>
        public void AddChild(PartNode node, string description = "")
        {
            this.ImportRejects.Add(new PartEdge(node, description));
        }

        /// <summary>
        /// Method to add a node that the current node caused a rejection error in.
        /// </summary>
        /// <param name="node">The node that the current node caused the error in.</param>
        /// <param name="description">Label to use when visualizing the edge.</param>
        public void AddParent(PartNode node, string description = "")
        {
            this.RejectsCaused.Add(new PartEdge(node, description));
        }

        /// <summary>
        /// Method to update the whitelisted propert of the current node.
        /// </summary>
        /// <param name="value">New value of the whitelist property.</param>
        public void SetWhiteListed(bool value)
        {
            this.IsWhiteListed = value;
        }

        /// <summary>
        /// Method to add error message to display as output.
        /// </summary>
        /// <param name="message">The text associated with the error.</param>
        public void AddErrorMessage(string message)
        {
            this.VerboseMessages.Add(message);
        }

        /// <summary>
        /// Method to check if the part exports any fields.
        /// </summary>
        /// <returns>A boolean indicating if the part exports any fields.</returns>
        public bool HasExports()
        {
            return this.ExportingContracts.Count > 0;
        }
    }
}
