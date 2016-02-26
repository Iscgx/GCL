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
        private readonly Parser _parser;
        private readonly Dictionary<int, Dictionary<Symbol, Tuple<ActionType, int>>> _actions;
        private readonly Dictionary<Node, int> _nodes;
        private readonly Dictionary<Production, int> _idByProduction;
        private readonly Dictionary<int, Production> _productionById;
        private readonly List<Tuple<ActionType, ActionType>> _errors; 
        private readonly Symbol _startSymbol;
        private readonly Node _head;

        public OnError OnError;

        public SyntaxTable(IList<Node> nodes, Node head, Parser parser, Symbol startSymbol)
        {
            if (nodes == null || parser == null)
                throw new ArgumentNullException();
            _parser = parser;
            _head = head;
            _actions = new Dictionary<int, Dictionary<Symbol, Tuple<ActionType, int>>>();
            _idByProduction = new Dictionary<Production, int>();
            _productionById = new Dictionary<int, Production>();
            _errors = new List<Tuple<ActionType, ActionType>>();
            _nodes = new Dictionary<Node, int>();

            for (var i = 0; i < nodes.Count(); i++)
                _nodes.Add(nodes[i], i);

            _startSymbol = startSymbol;

            FillProductions();
            FillTable();
        }

        private void FillProductions()
        {
            var i = 0;
            foreach (var element in _head.Footer)
            {
                _idByProduction.Add(element.Production, i);
                _productionById.Add(i, element.Production);
                i++;
            }
        }

        private void FillTable()
        {
            foreach (var node in _nodes.Select(e => e.Key))
            {
                _actions.Add(_nodes[node], new Dictionary<Symbol, Tuple<ActionType, int>>());
                if (node != _head && node.Kernel.Any(element => element.Production.Producer == _startSymbol))
                    _actions[_nodes[node]][_parser.Grammar.EndOfFile] = new Tuple<ActionType, int>(ActionType.Accept, 0); //Accept input
                foreach (var transition in node.Transitions)
                {
                    if (transition.Key.Type == SymbolType.NonTerminal)
                    {
                        
                        _actions[_nodes[node]][transition.Key] = new Tuple<ActionType, int>(ActionType.GoTo, _nodes[transition.Value]); //Goto Action
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
                            _actions[_nodes[node]][transition.Key] = new Tuple<ActionType, int>(ActionType.Shift, _nodes[transition.Value]); //Shift Action
                    }
                }
                foreach (var element in node.Kernel)
                {
                    if (element.ReadCompleted == false || element.Production.Producer == _startSymbol) 
                        continue;
                    foreach (var symbol in _parser.Grammar.Follow(element.Production.Producer))
                    {
                        if (_nodes.ContainsKey(node) && _actions.ContainsKey(_nodes[node]) && _actions[_nodes[node]].ContainsKey(symbol))
                        {
                            var action = _actions[_nodes[node]][symbol];
                            var error = new Tuple<ActionType, ActionType>(_actions[_nodes[node]][symbol].Item1,
                                                                          ActionType.Shift);
                            _errors.Add(error);
                            if (OnError != null)
                                OnError(error);
                        }
                        else
                        {
                            if (_idByProduction.ContainsKey(element.Production) == false)
                            {
                                _idByProduction.Add(element.Production, _productionById.Count);
                                _productionById.Add(_productionById.Count, element.Production);
                            }
                            _actions[_nodes[node]][symbol] = new Tuple<ActionType, int>(ActionType.Reduce,
                                                                                        _idByProduction[
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
                if (_actions[state].ContainsKey(symbol) == false)
                    return new Tuple<ActionType, int>(ActionType.Error, state);
                return _actions[state][symbol];
            }
        }

        public bool ContainsKey(int state, Symbol symbol)
        {
             return _actions[state].ContainsKey(symbol);
        }

        public Production ProductionById(int id)
        {
            return _productionById[id];
        }
   
    }
}
