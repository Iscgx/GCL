using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    public class StructureType : Type 
    {
        public Dictionary<string,Variable> Variables { get; set; }
        public string Name { get; set; }
        public bool AtDevice { get; set; }

        public StructureType(string name, Dictionary<string, Variable> variables, bool atDevice)
        {
            Name = name;
            AtDevice = atDevice;
            Variables = variables;
            Parameters = TypeParameters.IsStructure;
        }
    }
}
