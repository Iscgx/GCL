using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    public class Function
    {
        public List<Type> Parameters { get; set; }
        public string ReturnType { get; set; }
        public bool AtDevice { get; set; }
        public string Name { get; set; }

        public Function(string name, List<Type> parameters, string returntype, bool atDevice)
        {
            Name = name;
            Parameters = parameters;
            ReturnType = returntype;
            AtDevice = atDevice;
        }

        protected bool Equals(Function other)
        {

            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Parameters != null ? Parameters.GetHashCode() : 0) * 397) ^ (Name != null ? Name.GetHashCode() : 0);
            }
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != this.GetType()) return false;
            return Equals((Function) o);
        }
    }
}
