using System;
using System.Collections.Generic;
using System.Linq;
using Semantic;
using Token_Analizer;
using gcl2.Data;

namespace gcl2
{

    public class Parser
    {
        public Grammar Grammar { get; private set; }
        private readonly Node _head;
        private readonly Dictionary<Node, Node> _nodes;

        public SyntaxTable SyntaxTable { get; private set; }

        public Parser(Grammar grammar, Symbol initial)
        {
            Grammar = grammar;
            _nodes = new Dictionary<Node, Node>();
            _head = new Node();
            var start = grammar.NewSymbol(SymbolType.NonTerminal);
            var production = new Production(start, initial);
            grammar.Add(start, production);
            _head.Kernel.Add(production);
            Closure(_head);
            SyntaxTable = new SyntaxTable(new List<Node>(_nodes.Select(e => e.Key)), _head, this, start);
        }

        private void Closure(Node node)
        {
            if (_nodes.ContainsKey(node) == false)
                _nodes[node] = node;
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

                if (_nodes.ContainsKey(newNode) == false)
                {
                    _nodes.Add(newNode, newNode);
                    Closure(newNode);
                }
                else
                {
                    newNode = _nodes[newNode];
                }

                if (node.IsConnected(newNode) == false)
                    node.AddTransition(group.Key, newNode);
            }
        }

    }
}
