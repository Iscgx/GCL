using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    public interface ISymbolTable
    {
        Dictionary<string, Variable> Variables { get; set; }
        Dictionary<string, Function> Functions { get; set; }
        Dictionary<string, StructureType> Structures { get; set; }

        void AddVariable(Variable variable);
        bool HasVariable(Variable variable);
    }
}
