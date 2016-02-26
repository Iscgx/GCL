using System.Collections.Generic;

namespace Semantic
{
    public class Variable
    {
        protected bool Equals(Variable other)
        {
            return string.Equals(Name, other.Name);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        //Field
        public string Name { get; set; }
        public Type Type { get; set; }
        public bool AtDevice { get; set; }
        public bool IsConstant { get; set; }

        public Variable(string name, Type type, bool atDevice)
        {
            Name = name;
            Type = type;
            AtDevice = atDevice;
            IsConstant = false;
        }

        public override bool Equals(object o)
        {
            if (ReferenceEquals(null, o)) return false;
            if (ReferenceEquals(this, o)) return true;
            if (o.GetType() != this.GetType()) return false;
            return Equals((Variable) o);
        }
    }
}
