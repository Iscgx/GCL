using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    class Block
    {
        private uint tempVal;
        public Dictionary<string,Variable> Variables { get; set; }
        public Dictionary<string,StructureType> Structures { get; set; }
        
        public Block()
        {
            Variables = new Dictionary<string, Variable>();
            Structures = new Dictionary<string, StructureType>();
        }

        public bool HasVariable(Variable variable)
        {
            return Variables.ContainsKey(variable.Name);
        }

        public void AddVariable(Variable variable)
        {
           Variables.Add(variable.Name, variable);
        }

        public bool AddVariable(StructureType variable)
        {
            if (Structures.ContainsKey(variable.Name)) return false;
            Structures.Add(variable.Name,variable);
            return true;
        }

        public string GetNextTempName()
        {
            var r = "Temp_" + tempVal;
            tempVal += 1;
            return r;
        }
    }
}
