﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace VSMefx.Commands
{
    public class Command
    {
        protected ConfigCreator Creator { get; private set; }
        protected CLIOptions Options { get; private set; }

        public Command(ConfigCreator DerivedInfo, CLIOptions Arguments)
        {
            this.Creator = DerivedInfo;
            this.Options = Arguments;
        }

        protected ComposablePartDefinition getPart(string partName)
        {
            
            foreach(ComposablePartDefinition part in Creator.catalog.Parts)
            {
                if(part.Type.FullName.Equals(partName))
                {
                    return part; 
                }
            }
            return null;
        }

        protected string getName(ComposablePartDefinition part, string verboseLabel = "")
        {
            Type type = part.Type;
            if (this.Options.verbose)
            {
                return verboseLabel + " " + type.AssemblyQualifiedName;
            }
            else
            {
                return type.FullName;
            }
        }

    }

    public class CommandData
    {
        string Symbol { get; }
    }
}
