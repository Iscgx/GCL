using System.Collections.Generic;
using System.Linq;
using GCL.Syntax.Data;
using Semantic;

namespace GCL.Syntax
{

    public class Parser
    {
        public Grammar Grammar { get; private set; }
        private readonly Dictionary<Node, Node> nodes;

        public SyntaxTable SyntaxTable { get; private set; }

        public Parser(Grammar grammar, Symbol initial)
        {
            Grammar = grammar;
            nodes = new Dictionary<Node, Node>();
            var head = new Node();
            var start = grammar.NewSymbol(SymbolType.NonTerminal);
            var production = new Production(start, initial);
            grammar.Add(start, production);
            head.Kernel.Add(production);
            Closure(head);
            SyntaxTable = new SyntaxTable(new List<Node>(nodes.Select(e => e.Key)), head, this, start);
        }

        private void Closure(Node node)
        {
            if (nodes.ContainsKey(node) == false)
                nodes[node] = node;
            var footer = node.Footer;
            var usedSymbols = new HashSet<Symbol>();
            var availableElements = new Stack<Element>(node.Kernel);
            while (availableElements.Count != 0)
            {
                var element = availableElements.Pop();

                if (element.ReadIndex < element.Production.Product.Count)
                {
                    var readSymbol = element.ReadSymbol;
                    if (usedSymbols.Contains(readSymbol) == false && readSymbol.Type == SymbolType.NonTerminal)
                    {
                        usedSymbols.Add(readSymbol);
                        foreach (var production in Grammar[readSymbol])
                        {
                            var productionToElement = new Element(production);
                            if (footer.Has(productionToElement) == false)
                            {
                                availableElements.Push(productionToElement);
                                footer.Add(production);
                            }

                        }
                    }
                }
            }
            CreateConnections(node);
        }

        private void CreateConnections(Node node)
        {
            var grouping = new Dictionary<Symbol, List<Element>>();
            foreach (var element in node)
            {
                if (element.ReadIndex < element.Production.Product.Count)
                {
                    var readSymbol = element.ReadSymbol;
                    if (grouping.ContainsKey(readSymbol) == false)
                        grouping.Add(readSymbol, new List<Element>());
                    grouping[readSymbol].Add(element.Read());
                }
            }
            foreach (var group in grouping)
            {
                var newNode = new Node(group.Value);

                if (nodes.ContainsKey(newNode) == false)
                {
                    nodes.Add(newNode, newNode);
                    Closure(newNode);
                }
                else
                {
                    newNode = nodes[newNode];
                }

                if (node.IsConnected(newNode) == false)
                    node.AddTransition(group.Key, newNode);
            }
        }

    }
}
