using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    public class Attribute
    {
        public string Name { get; private set; }

        public int? Value { get; set; }

        public Attribute(string name, int? value = null)
        {
            Name = name;
            Value = value;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is Attribute) == false)
                return false;
            var otherAttribute = (Attribute)obj;
            if (otherAttribute.Name == Name)
                return true;
            return false;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
