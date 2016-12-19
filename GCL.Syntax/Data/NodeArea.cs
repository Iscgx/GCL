using System;
using System.Collections.Generic;
using System.Linq;

namespace GCL.Syntax.Data
{
    public class NodeArea : HashSet<Element>
    {
        private int hashCode = 0;

        public NodeArea()
        {
        }

        public NodeArea(IEnumerable<Element> collection) : base(collection)
        {

        }

        public override int GetHashCode()
        {
            if (this.hashCode == 0)
                this.hashCode = this.Aggregate(0, (current, element) => current ^ (486187739 & element.GetHashCode()));
            return this.hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is NodeArea) == false)
                return false;
            var otherNodeArea = obj as NodeArea;
            return SetEquals(otherNodeArea);
        }

        public static bool operator ==(NodeArea n1, NodeArea n2)
        {
            if ((object) n1 == null || (object) n2 == null)
                return false;
            return n1.Equals(n2);
        }

        public static bool operator !=(NodeArea n1, NodeArea n2)
        {
            return !(n1 == n2);
        }

        public override string ToString()
        {
            return $"Count = {Count}";
        }
    }
}
