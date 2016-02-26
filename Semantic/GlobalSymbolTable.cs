using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    class GlobalSymbolTable : ISymbolTable
    {
        public Dictionary<string, Variable> Variables { get; set; }
        public Dictionary<string, Function> Functions { get; set; }
        public Dictionary<string, StructureType> Structures { get; set; }
        public DeviceSymbolTable DeviceSymbolTable { get; set; }

        public GlobalSymbolTable()
        {
            Variables = new Dictionary<string, Variable>();
            Structures = new Dictionary<string, StructureType>();
            DeviceSymbolTable = new DeviceSymbolTable();
            Functions = new Dictionary<string, Function>
            {
                //{"print", new Function("print", new List<string> {"string", "..."}, "void", false)},
                //{"read", new Function("read", new List<string> {"string", "..."}, "void", false)}
            };
        }

        public void AddVariable(Variable variable)
        {
            if(variable.AtDevice)
                DeviceSymbolTable.AddVariable(variable);
            else
                Variables.Add(variable.Name, variable);
        }

        public bool HasVariable(Variable variable)
        {
            return Variables.ContainsKey(variable.Name);
        }

        public bool AddVariable(StructureType variable)
        {
            if (Structures.ContainsKey(variable.Name)) return false;
            if (variable.AtDevice == true)
            {
                return DeviceSymbolTable.AddVariable(variable);
            }
            else
            {
                Structures.Add(variable.Name, variable);
                return true;
            }
        }

        public bool AddFunction(string name, Function function)
        {
            if(function.AtDevice == true)
            {
                return DeviceSymbolTable.AddFunction(name, function);
            }
            else
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
}
