using System;
using System.Collections.Generic;
using System.Linq;
using GCL.Syntax.Data;
using Semantic;

namespace GCL.Syntax
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
            this.actions = new Dictionary<int, Dictionary<Symbol, Tuple<ActionType, int>>>();
            this.idByProduction = new Dictionary<Production, int>();
            this.productionById = new Dictionary<int, Production>();
            this.errors = new List<Tuple<ActionType, ActionType>>();
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
            foreach (var element in this.head.Footer)
            {
                this.idByProduction.Add(element.Production, i);
                this.productionById.Add(i, element.Production);
                i++;
            }
        }

        private void FillTable()
        {
            foreach (var node in this.nodes.Select(e => e.Key))
            {
                this.actions.Add(this.nodes[node], new Dictionary<Symbol, Tuple<ActionType, int>>());
                if (node != this.head && node.Kernel.Any(element => element.Production.Producer == this.startSymbol))
                {
                    this.actions[this.nodes[node]][this.parser.Grammar.EndOfFile] = new Tuple<ActionType, int>(ActionType.Accept, 0); //Accept input
                }
                foreach (var transition in node.Transitions)
                {
                    if (transition.Key.Type == SymbolType.NonTerminal)
                    {
                        this.actions[this.nodes[node]][transition.Key] = new Tuple<ActionType, int>(ActionType.GoTo, this.nodes[transition.Value]); //Goto Action
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
                        this.actions[this.nodes[node]][transition.Key] = new Tuple<ActionType, int>(ActionType.Shift, this.nodes[transition.Value]); //Shift Action
                    }
                }
                foreach (var element in node.Kernel)
                {
                    if (element.ReadCompleted == false || element.Production.Producer == this.startSymbol) 
                        continue;
                    foreach (var symbol in this.parser.Grammar.Follow(element.Production.Producer))
                    {
                        if (this.nodes.ContainsKey(node) && this.actions.ContainsKey(this.nodes[node]) && this.actions[this.nodes[node]].ContainsKey(symbol))
                        {
                            var action = this.actions[this.nodes[node]][symbol];
                            var error = new Tuple<ActionType, ActionType>(this.actions[this.nodes[node]][symbol].Item1,
                                                                          ActionType.Shift);
                            this.errors.Add(error);
                            this.OnError?.Invoke(error);
                        }
                        else
                        {
                            if (this.idByProduction.ContainsKey(element.Production) == false)
                            {
                                this.idByProduction.Add(element.Production, this.productionById.Count);
                                this.productionById.Add(this.productionById.Count, element.Production);
                            }
                            this.actions[this.nodes[node]][symbol] = new Tuple<ActionType, int>(ActionType.Reduce, this.idByProduction[
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
                if (this.actions[state].ContainsKey(symbol) == false)
                    return new Tuple<ActionType, int>(ActionType.Error, state);
                return this.actions[state][symbol];
            }
        }

        public bool ContainsKey(int state, Symbol symbol)
        {
             return this.actions[state].ContainsKey(symbol);
        }

        public Production ProductionById(int id)
        {
            return this.productionById[id];
        }
   
    }
}
