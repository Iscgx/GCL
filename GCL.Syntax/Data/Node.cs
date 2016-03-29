using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Semantic;

namespace GCL.Syntax.Data
{
    public class Node : IEnumerable<Element>
    {
        private readonly HashSet<Node> neighbors;

        public NodeArea Kernel { get; }

        public NodeArea Footer { get; }

        public Dictionary<Symbol, Node> Transitions { get; }

        public Node()
        {
            Kernel = new NodeArea();
            Footer = new NodeArea();
            Transitions = new Dictionary<Symbol, Node>();
            neighbors = new HashSet<Node>();
        }

        public Node(IEnumerable<Element> kernel)
        {
            this.Kernel = new NodeArea(kernel);
            Footer = new NodeArea();
            Transitions = new Dictionary<Symbol, Node>();
            neighbors = new HashSet<Node>();
        }

        public bool IsConnected(Node otherNode)
        {
            return neighbors.Contains(otherNode);
        }

        public void AddTransition(Symbol symbol, Node otherNode)
        {
            Transitions[symbol] = otherNode;
            neighbors.Add(otherNode);
        }

        public override int GetHashCode()
        {
            return Kernel.GetHashCode() ^ Footer.GetHashCode();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (obj is Node) == false)
                return false;
            var otherNode = obj as Node;
            return Kernel == otherNode.Kernel;
        }

        public static bool operator ==(Node n1, Node n2)
        {
            if ((object)n1 == null || (object)n2 == null)
                return false;
            return n1.Equals(n2);
        }

        public static bool operator !=(Node n1, Node n2)
        {
            return !(n1 == n2);
        }

        public IEnumerator<Element> GetEnumerator()
        {
            return Kernel.Union(Footer).GetEnumerator();
        }

        public override string ToString()
        {
            return GetHashCode().ToString(CultureInfo.InvariantCulture);
        }
    }
}
