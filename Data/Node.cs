using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Semantic;

namespace gcl2.Data
{
    public class Node : IEnumerable<Element>
    {
        private readonly NodeArea kernel;
        private readonly NodeArea footer;
        private readonly HashSet<Node> neighbors;
        private readonly Dictionary<Symbol, Node> transtions;

        public NodeArea Kernel => kernel;

        public NodeArea Footer => footer;

        public Dictionary<Symbol, Node> Transitions => new Dictionary<Symbol, Node>(transtions);

        public Node()
        {
            kernel = new NodeArea();
            footer = new NodeArea();
            transtions = new Dictionary<Symbol, Node>();
            neighbors = new HashSet<Node>();
        }

        public Node(IEnumerable<Element> kernel)
        {
            this.kernel = new NodeArea(kernel);
            footer = new NodeArea();
            transtions = new Dictionary<Symbol, Node>();
            neighbors = new HashSet<Node>();
        }

        public bool IsConnected(Node otherNode)
        {
            return neighbors.Contains(otherNode);
        }

        public void AddTransition(Symbol symbol, Node otherNode)
        {
            transtions[symbol] = otherNode;
            neighbors.Add(otherNode);
        }

        public override int GetHashCode()
        {
            return kernel.GetHashCode() ^ footer.GetHashCode();
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
            return kernel == otherNode.kernel;
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
