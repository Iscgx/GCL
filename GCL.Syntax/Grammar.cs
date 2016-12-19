using System;
using System.Collections.Generic;
using System.Linq;
using GCL.Syntax.Data;
using Semantic;
using Attribute = Semantic.Attribute;

namespace GCL.Syntax
{    
    public class Grammar : IEnumerable<List<Production>>
    {
        private readonly Dictionary<Symbol, List<Production>> productions;
        private readonly Dictionary<Symbol, HashSet<Symbol>> calculatedFirsts;
        private readonly Dictionary<Symbol, HashSet<Symbol>> calculatedFollows;
        private readonly Dictionary<Symbol, HashSet<Production>> products;
        private readonly List<Symbol> terminals;
        private readonly List<Symbol> nonTerminals;
        private bool hasCalculatedFollows = false;
        private int symbolIdentity = 1;

        public Symbol Epsilon { get; }

        public Symbol EndOfFile { get; }

        public Grammar()
        {
            this.productions = new Dictionary<Symbol, List<Production>>();
            Epsilon = new Symbol(SymbolType.Epsilon, 0);
            EndOfFile = new Symbol(SymbolType.EndOfFile, -1);
            this.calculatedFirsts = new Dictionary<Symbol, HashSet<Symbol>>();
            this.products = new Dictionary<Symbol, HashSet<Production>>();
            this.calculatedFollows = new Dictionary<Symbol, HashSet<Symbol>>();
            this.terminals = new List<Symbol>();
            this.nonTerminals = new List<Symbol>();
            
        }

        public Symbol NewSymbol(SymbolType type, List<Attribute> properties = null)
        {
            if (type == SymbolType.Epsilon)
                return Epsilon;
            if (type == SymbolType.EndOfFile)
                return EndOfFile;
            var symbol = new Symbol(type, this.symbolIdentity, properties);
            if (type == SymbolType.NonTerminal)
                this.nonTerminals.Add(symbol);
            else if (type == SymbolType.Terminal)
                this.terminals.Add(symbol);

            this.symbolIdentity += 1;
            return symbol;
        }

        public void Add(Symbol producer)
        {
            if (this.productions.ContainsKey(producer) == false)
                this.productions.Add(producer, new List<Production>());
        }

        public void Add(Symbol producer, Production production, params Production[] extraProductions)
        {
            Add(producer);
            this.productions[producer].Add(production);
            Relate(production);
            foreach (var prod in extraProductions)
            {
                this.productions[producer].Add(prod);
                Relate(prod);
            }
        }

        public bool Has(Symbol producer)
        {
            return this.productions.ContainsKey(producer);
        }

        public List<Production> Produces(Symbol symbol)
        {
            if (this.products.ContainsKey(symbol) == false)
                return new List<Production>();
            return new List<Production>(this.products[symbol]);
        }

        public HashSet<Symbol> Follow(Symbol symbol)
        {
            if (this.hasCalculatedFollows == false)
            {
                CalculateFollows();
                this.hasCalculatedFollows = true;
            }
            return this.calculatedFollows[symbol];
        }

        private void CalculateFollows()
        {
            var firstsToAdd = new List<Tuple<Symbol, List<List<Symbol>>>>();
            var followsToAdd = new List<Tuple<Symbol, Symbol>>();
            foreach (var nonTerminal in this.nonTerminals)
            {
                this.calculatedFollows[nonTerminal] = new HashSet<Symbol>();
                if (nonTerminal.Id == 1)
                    this.calculatedFollows[nonTerminal].Add(EndOfFile);
                foreach (var production in Produces(nonTerminal))
                {
                    for (var i = 0; i < production.Product.Count; i++)
                    {
                        if (production.Product[i] != nonTerminal) continue;
                        if (i == (production.Product.Count - 1)) //beta is empty. Nullable(beta) = true. missing emptystring support.
                        {
                            var producer = production.Producer;
                            if (producer != nonTerminal)
                            {
                                //add constraint follow(producer) is subset of Follow(N)
                                followsToAdd.Add(new Tuple<Symbol, Symbol>(nonTerminal, producer));
                            }
                        }
                        else //beta is not empty
                        {
                            var beta = new List<Symbol>();
                            for (var j = i + 1; j < production.Product.Count; j++)
                                beta.Add(production.Product[j]);
                            //add constraint firstBeta is subset of Follow(N)
                            var temp = new Tuple<Symbol, List<List<Symbol>>>(nonTerminal, new List<List<Symbol>>());
                            temp.Item2.Add(beta);
                            firstsToAdd.Add(temp);
                        }
                    }
                }
            }
            
            foreach (var tuple in firstsToAdd)
            {
                foreach (var firstBeta in tuple.Item2.Select(First))
                {
                    this.calculatedFollows[tuple.Item1].UnionWith(firstBeta);
                }
            }

            while (true)
            {
                var changed = false;
                foreach (var tuple in followsToAdd)
                {
                    var pastCount = this.calculatedFollows[tuple.Item1].Count;
                    var followProducer = this.calculatedFollows[tuple.Item2];
                    this.calculatedFollows[tuple.Item1].UnionWith(followProducer);
                    if (pastCount != this.calculatedFollows[tuple.Item1].Count)
                        changed = true;
                }
                if (changed == false)
                    break;
            }
        }

        public HashSet<Symbol> First(Symbol symbol)
        {
            if (this.calculatedFirsts.ContainsKey(symbol) == true)
                return new HashSet<Symbol>(this.calculatedFirsts[symbol]);
            return First(new List<Symbol>() { symbol });
        }

        public HashSet<Symbol> First(IEnumerable<Symbol> symbols)
        {
            var firstSymbols = new HashSet<Symbol>();
            First(Epsilon, symbols, firstSymbols);
            return new HashSet<Symbol>(firstSymbols);
        }

        private void First(Symbol caller, IEnumerable<Symbol> symbols, ISet<Symbol> firstSet)
        {
            foreach (var symbol in symbols.TakeWhile(symbol => symbol != caller))
            {
                if (this.calculatedFirsts.ContainsKey(symbol))
                {
                    firstSet.UnionWith(this.calculatedFirsts[symbol]);
                    //firstSet.Add(symbol);
                }
                else
                {
                    if (symbol.Type == SymbolType.Terminal || symbol.Type == SymbolType.Epsilon)
                    {
                        this.calculatedFirsts[symbol] = new HashSet<Symbol> { symbol };
                        firstSet.Add(symbol);
                    }
                    else
                    {
                        foreach (var production in this.productions[symbol])
                        {
                            var newFirstSet = new HashSet<Symbol>();
                            First(symbol, production.Product, newFirstSet);
                            firstSet.UnionWith(newFirstSet);
                        }
                    }
                }

                if (this.calculatedFirsts.ContainsKey(symbol) == false)
                    this.calculatedFirsts[symbol] = new HashSet<Symbol>(firstSet);
                if (firstSet.Contains(Epsilon))
                    continue;
                break;
            }
        }

        private void Relate(Production production)
        {
            foreach (var symbol in production.Product)
            {
                if (this.products.ContainsKey(symbol) == false)
                    this.products[symbol] = new HashSet<Production>();
                this.products[symbol].Add(production);
            }
        }

        public List<Production> this[Symbol producer] => this.productions[producer];

        public IEnumerator<List<Production>> GetEnumerator()
        {
            return this.productions.Select(production => production.Value).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.productions.Select(production => production.Value).GetEnumerator();
        }
    }
}
