using System;
using System.Collections.Generic;

namespace gcl2
{
    public struct Symbol
    {
        //private static Dictionary<int, string> names = new Dictionary<int, string>()
        //    {
        //        {1, "E"},
        //        {2, "T"},
        //        {3, "F"},
        //        {4, "+"},
        //        {5, "*"},
        //        {6, "("},
        //        {7, ")"},
        //        {8, "id"},
        //        {9, "E'"},
        //        {0, "e"},
        //        {-1, "eof"}
        //    };

        public SymbolType Type;
        public readonly int Id;

        public Symbol(SymbolType type, int id)
        {
            Type = type;
            Id = id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || (obj is ValueType) == false || (obj is Symbol) == false)
                return false;
            var otherSymbol = (Symbol) obj;
            if (otherSymbol.Id == Id && otherSymbol.Type == Type)
                return true;
            return false;
        }

        public override string ToString()
        {
            return string.Format("[{0}]", Id);
        }

        public static bool operator ==(Symbol s1, Symbol s2)
        {
            return s1.Equals(s2);
        }

        public static bool operator !=(Symbol s1, Symbol s2)
        {
            return !(s1 == s2);
        }

    }

    public enum SymbolType
    {
        Terminal,
        NonTerminal,
        Epsilon,
        EndOfFile
    }
}
