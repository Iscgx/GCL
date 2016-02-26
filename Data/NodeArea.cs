using System;
using System.Collections.Generic;
using System.Linq;

namespace gcl2.Data
{
    public class NodeArea : IEnumerable<Element>
    {
        private readonly HashSet<Element> _elements;
        private int _hashCode = 0;
        public NodeArea()
        {
            _elements = new HashSet<Element>();
        }

        public NodeArea(IEnumerable<Element> collection)
        {
            _elements = new HashSet<Element>(collection);
        }

        public void Add(Element element)
        {
            if (element == null)
// ReSharper disable NotResolvedInText
                throw new ArgumentNullException("element cannot be null.");
// ReSharper restore NotResolvedInText
            _elements.Add(element);

        }

        public void Add(Production production)
        {
            var element = new Element(production);
            Add(element);

        }

        public bool Has(Element element)
        {
            if (element == null)
                return false;
            return _elements.Contains(element);
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
                _hashCode = _elements.Aggregate(0, (current, element) => current ^ (486187739 & element.GetHashCode()));
            return _hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is NodeArea) == false)
                return false;
            var otherNodeArea = obj as NodeArea;
            return _elements.SetEquals(otherNodeArea._elements);
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
            return string.Format("Count = {0}", _elements.Count);
        }

        public IEnumerator<Element> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _elements.GetEnumerator();
        }
    }
}
