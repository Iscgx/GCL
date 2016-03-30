using System;
using System.Collections.Generic;
using System.Linq;

namespace GCL.Syntax.Data
{
    public class NodeArea : IEnumerable<Element>
    {
        private readonly HashSet<Element> elements;
        private int hashCode = 0;

        public NodeArea()
        {
            elements = new HashSet<Element>();
        }

        public NodeArea(IEnumerable<Element> collection)
        {
            elements = new HashSet<Element>(collection);
        }

        public void Add(Element element)
        {
            elements.Add(element);
        }

        public bool Has(Element element)
        {
            if (element == null)
                return false;
            return elements.Contains(element);
        }

        public override int GetHashCode()
        {
            if (hashCode == 0)
                hashCode = elements.Aggregate(0, (current, element) => current ^ (486187739 & element.GetHashCode()));
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is NodeArea) == false)
                return false;
            var otherNodeArea = obj as NodeArea;
            return elements.SetEquals(otherNodeArea.elements);
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

        public static NodeArea operator +(NodeArea n1, NodeArea n2)
        {
            return new NodeArea(n1.Union(n2));
        }

        public override string ToString()
        {
            return $"Count = {elements.Count}";
        }

        public IEnumerator<Element> GetEnumerator()
        {
            return elements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return elements.GetEnumerator();
        }
    }
}
