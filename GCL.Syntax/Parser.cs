using System.Collections.Generic;
using System.Linq;
using GCL.Syntax.Data;
using Semantic;

namespace GCL.Syntax
{

    public class Parser
    {
        public Grammar Grammar { get; }
        private readonly Dictionary<Node, Node> nodes = new Dictionary<Node, Node>();

        public SyntaxTable SyntaxTable { get; private set; }

        public Parser(Grammar grammar)
        {
            Grammar = grammar;
            var startSymbol = grammar.NewSymbol(SymbolType.NonTerminal);
            var production = new Production(startSymbol, new [] { new Symbol(SymbolType.NonTerminal, 1) });
            grammar.Add(startSymbol, production);

            var head = new Node(new[] {new Element(production)});
            Closure(head);
            SyntaxTable = new SyntaxTable(nodes.Keys.ToList(), head, this, startSymbol);
        }

        private void Closure(Node node)
        {
            if (nodes.ContainsKey(node) == false)
                nodes[node] = node;
            var usedSymbols = new HashSet<Symbol>();
            var availableElements = new Stack<Element>(node.Kernel);
            while (availableElements.Count != 0)
            {
                var element = availableElements.Pop();

                if (element.ReadIndex >= element.Production.Product.Count)
                {
                    continue;
                }

                var readSymbol = element.ReadSymbol;

                if (usedSymbols.Contains(readSymbol) || readSymbol.Type != SymbolType.NonTerminal)
                {
                    continue;
                }

                usedSymbols.Add(readSymbol);

                foreach (var production in Grammar[readSymbol])
                {
                    var productionToElement = new Element(production);
                    if (node.Footer.Contains(productionToElement) == false)
                    {
                        availableElements.Push(productionToElement);
                        node.Footer.Add(new Element(production));
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
