using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Semantic;
using gcl2.Data;

namespace gcl2
{
    public enum ActionType
    {
        Shift,
        Reduce,
        Accept,
        GoTo,
        Error
    }

    public delegate void OnError(Tuple<ActionType, ActionType> error);

    public class SyntaxTable
    {
        private readonly Parser parser;
        private readonly Dictionary<int, Dictionary<Symbol, Tuple<ActionType, int>>> actions;
        private readonly Dictionary<Node, int> nodes;
        private readonly Dictionary<Production, int> idByProduction;
        private readonly Dictionary<int, Production> productionById;
        private readonly List<Tuple<ActionType, ActionType>> errors; 
        private readonly Symbol startSymbol;
        private readonly Node head;

        public OnError OnError;

        public SyntaxTable(IList<Node> nodes, Node head, Parser parser, Symbol startSymbol)
        {
            if (nodes == null || parser == null)
                throw new ArgumentNullException();
            this.parser = parser;
            this.head = head;
            actions = new Dictionary<int, Dictionary<Symbol, Tuple<ActionType, int>>>();
            idByProduction = new Dictionary<Production, int>();
            productionById = new Dictionary<int, Production>();
            errors = new List<Tuple<ActionType, ActionType>>();
            this.nodes = new Dictionary<Node, int>();

            for (var i = 0; i < nodes.Count(); i++)
                this.nodes.Add(nodes[i], i);

            this.startSymbol = startSymbol;

            FillProductions();
            FillTable();
        }

        private void FillProductions()
        {
            var i = 0;
            foreach (var element in head.Footer)
            {
                idByProduction.Add(element.Production, i);
                productionById.Add(i, element.Production);
                i++;
            }
        }

        private void FillTable()
        {
            foreach (var node in nodes.Select(e => e.Key))
            {
                actions.Add(nodes[node], new Dictionary<Symbol, Tuple<ActionType, int>>());
                if (node != head && node.Kernel.Any(element => element.Production.Producer == startSymbol))
                    actions[nodes[node]][parser.Grammar.EndOfFile] = new Tuple<ActionType, int>(ActionType.Accept, 0); //Accept input
                foreach (var transition in node.Transitions)
                {
                    if (transition.Key.Type == SymbolType.NonTerminal)
                    {
                        
                        actions[nodes[node]][transition.Key] = new Tuple<ActionType, int>(ActionType.GoTo, nodes[transition.Value]); //Goto Action
                    }
                    else if (transition.Key.Type != SymbolType.EndOfFile)
                    {
                        //if (_actions[_nodes[node]][transition.Key] != null)
                        //{
                        //    var error = new Tuple<ActionType, ActionType>(_actions[_nodes[node]][transition.Key].Item1, ActionType.Shift);
                        //    _errors.Add(error);
                        //    if (OnError != null)
                        //        OnError(error);
                        //}
                        //else
                            actions[nodes[node]][transition.Key] = new Tuple<ActionType, int>(ActionType.Shift, nodes[transition.Value]); //Shift Action
                    }
                }
                foreach (var element in node.Kernel)
                {
                    if (element.ReadCompleted == false || element.Production.Producer == startSymbol) 
                        continue;
                    foreach (var symbol in parser.Grammar.Follow(element.Production.Producer))
                    {
                        if (nodes.ContainsKey(node) && actions.ContainsKey(nodes[node]) && actions[nodes[node]].ContainsKey(symbol))
                        {
                            var action = actions[nodes[node]][symbol];
                            var error = new Tuple<ActionType, ActionType>(actions[nodes[node]][symbol].Item1,
                                                                          ActionType.Shift);
                            errors.Add(error);
                            if (OnError != null)
                                OnError(error);
                        }
                        else
                        {
                            if (idByProduction.ContainsKey(element.Production) == false)
                            {
                                idByProduction.Add(element.Production, productionById.Count);
                                productionById.Add(productionById.Count, element.Production);
                            }
                            actions[nodes[node]][symbol] = new Tuple<ActionType, int>(ActionType.Reduce,
                                                                                        idByProduction[
                                                                                            element.Production]);
                                //Reduce
                        }
                    }
                }
            }
        }

        public Tuple<ActionType, int> this[int state, Symbol symbol]
        {
            get
            {
                if (actions[state].ContainsKey(symbol) == false)
                    return new Tuple<ActionType, int>(ActionType.Error, state);
                return actions[state][symbol];
            }
        }

        public bool ContainsKey(int state, Symbol symbol)
        {
             return actions[state].ContainsKey(symbol);
        }

        public Production ProductionById(int id)
        {
            return productionById[id];
        }
   
    }
}
