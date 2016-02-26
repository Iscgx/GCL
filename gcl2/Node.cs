using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace gcl2
{
    public class Node : IEnumerable<Element>
    {
        private readonly NodeArea _kernel;
        private readonly NodeArea _footer;
        private readonly HashSet<Node> _neighbors;
        private readonly Dictionary<Symbol, Node> _transtions;

        public NodeArea Kernel
        {
            get { return _kernel; }
        }

        public NodeArea Footer
        {
            get { return _footer; }
        }

        public Dictionary<Symbol, Node> Transitions
        {
            get
            {
                return new Dictionary<Symbol, Node>(_transtions);
            }
        }

        public Node()
        {
            _kernel = new NodeArea();
            _footer = new NodeArea();
            _transtions = new Dictionary<Symbol, Node>();
            _neighbors = new HashSet<Node>();
        }

        public Node(IEnumerable<Element> kernel)
        {
            _kernel = new NodeArea(kernel);
            _footer = new NodeArea();
            _transtions = new Dictionary<Symbol, Node>();
            _neighbors = new HashSet<Node>();
        }

        public bool IsConnected(Node otherNode)
        {
            return _neighbors.Contains(otherNode);
        }

        public void AddTransition(Symbol symbol, Node otherNode)
        {
            _transtions[symbol] = otherNode;
            _neighbors.Add(otherNode);
        }

        public override int GetHashCode()
        {
            return _kernel.GetHashCode() + _footer.GetHashCode();
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
            return _kernel == otherNode._kernel;
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
