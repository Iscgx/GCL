using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    class DeviceSymbolTable : ISymbolTable
    {
        public Dictionary<string, Variable> Variables { get; set; }
        public Dictionary<string, StructureType> Structures { get; set; }
        public Dictionary<string, DeviceFunction> DeviceFunctions { get; set; }
        public Dictionary<string, Function> Functions { get; set; }

        public DeviceSymbolTable()
        {
            Variables = new Dictionary<string, Variable>();
            Structures = new Dictionary<string, StructureType>();
            DeviceFunctions = new Dictionary<string, DeviceFunction>();
            Functions = new Dictionary<string, Function>();
        }

        public void AddVariable(Variable variable)
        {
            Variables.Add(variable.Name, variable);
        }

        public bool HasVariable(Variable variable)
        {
            return Variables.ContainsKey(variable.Name);
        }

        public bool AddVariable(StructureType variable)
        {
            if (Structures.ContainsKey(variable.Name)) return false;
            Structures.Add(variable.Name, variable);
            return true;
        }

        public bool AddFunction(string name, Function function)
        {
            if (Functions.ContainsKey(name))
                return false;
            else
            {
                Functions.Add(name, function);
                return true;
            }
        }
    }
}
