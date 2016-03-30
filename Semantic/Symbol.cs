using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace Semantic
{
    public class Symbol : ICloneable
    {
        public SymbolType Type { get; }
        public int Id { get; }
        public Dictionary<string, Attribute> Properties { get; } = new Dictionary<string, Attribute>();
        public dynamic Attributes { get; }

        public Symbol(SymbolType type, int id, IEnumerable<Attribute> properties = null)
        {
            Attributes = new ExpandoObject();
            Type = type;
            Id = id;
            
            if (properties != null)
            {
                foreach (var attribute in properties)
                {
                    Properties.Add(attribute.Name, attribute);
                }
            }
  
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public object Clone()
        {
            var clonedSymbol = new Symbol(Type, Id, Properties.Values.ToArray());
            foreach (var keyValuePair in (IDictionary<string, object>)Attributes)
            {
                ((IDictionary<string, object>)clonedSymbol.Attributes).Add(keyValuePair);
            }

            return clonedSymbol;
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || (obj is Symbol) == false)
                return false;
            var otherSymbol = (Symbol) obj;
            if (otherSymbol.Id == Id && otherSymbol.Type == Type)
                return true;
            return false;
        }

        public override string ToString()
        {
            return $"[{Id}]";
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
}
