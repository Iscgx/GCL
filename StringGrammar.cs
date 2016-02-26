using System;
using System.Collections.Generic;
using System.Linq;
using Token_Analizer;
using gcl2.Data;
using gcl2.Dynamic;
using Semantic;
using Attribute = Semantic.Attribute;

namespace gcl2
{
    public class StringGrammar
    {
        public Grammar Grammar { get; private set; }
        public readonly Dictionary<string, Symbol> SymbolTable;
        private readonly Dictionary<Attribute, Dictionary<Symbol, Symbol>> _symbolsByAttributes;
        private Dictionary<Symbol, string> _nameBySymbol;
        public Dictionary<int, int> TokenDictionary { get; private set; }
        public List<string> TokenNames { get; private set; }

        private bool _insideProduction;
        private Symbol _currentProducer;
        private List<Symbol> _currentProduct;
        private readonly List<Attribute> _attributes;
        private readonly DynamicCodeProvider _dynamicCode;
        private readonly Dictionary<Production, string> _semanticMethods;
        private string _currentSemanticMethod = null;

        public StringGrammar(IEnumerable<string> tokenNames, DynamicCodeProvider dynamicCode, Dictionary<Production, string> semanticMethods)
        {
            Grammar = new Grammar();
            SymbolTable = new Dictionary<string, Symbol>();
            TokenNames = tokenNames.ToList();
            TokenDictionary = new Dictionary<int, int>();

            _attributes = new List<Attribute>();
            _symbolsByAttributes = new Dictionary<Attribute, Dictionary<Symbol, Symbol>>();
            _dynamicCode = dynamicCode;
            _semanticMethods = semanticMethods;
        }

        public void AddSymbolDefinition(Token token)
        {
            var newToken = new Token(token.Type, token.Lexeme.Replace(@"\", ""), token.Message);
            if (token.CsharpCode == true)
            {
                _currentSemanticMethod = _dynamicCode.AddMethod(token.Lexeme);
                return;
            }
            if (token.Lexeme.StartsWith("#"))
            {
                var lexeme = token.Lexeme.Substring(1).Split(":".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                if (lexeme.Length == 1)
                {
                    _attributes.Add(new Attribute(lexeme[0]));
                }
                else
                {
                    var parseValue = 0;
                    var parsed = int.TryParse(lexeme[1], out parseValue);
                    if (parsed == true)
                        _attributes.Add(new Attribute(lexeme[0], parseValue));
                    else
                        _attributes.Add(new Attribute(lexeme[0]));
                }
                
                return;
            }
            if (token.Lexeme != "|" && token.Lexeme != ":" && _attributes.Count != 0 && SymbolTable.ContainsKey(newToken.Lexeme) == true)
            {
                foreach (var attribute in _attributes)
                {
                    if (_symbolsByAttributes.ContainsKey(attribute) == false)
                        _symbolsByAttributes[attribute] = new Dictionary<Symbol, Symbol>();
                    _symbolsByAttributes[attribute].Add(SymbolTable[newToken.Lexeme], SymbolTable[newToken.Lexeme]);
                    SymbolTable[newToken.Lexeme].Properties.Add(attribute.Name, attribute);
                }
                _attributes.Clear();
            }
            if (token.Lexeme != "|" && token.Lexeme != ":" && SymbolTable.ContainsKey(newToken.Lexeme) == false)
            {
                if (token.Lexeme == "endOfFile")
                {
                    SymbolTable.Add("endOfFile", Grammar.EndOfFile);
                }
                else
                {
                    var type = SymbolType.Terminal;
                    if (char.IsUpper(newToken.Lexeme.First()))
                        type = SymbolType.NonTerminal;
                    var symbol = Grammar.NewSymbol(type, _attributes);
                    SymbolTable.Add(newToken.Lexeme, symbol); //add symbol
                    //Console.WriteLine( "{0} : {1}",token.Lexeme, symbol.Id);
                    foreach (var attribute in _attributes)
                    {
                        if (_symbolsByAttributes.ContainsKey(attribute) == false)
                            _symbolsByAttributes[attribute] = new Dictionary<Symbol, Symbol>();
                        _symbolsByAttributes[attribute].Add(symbol, symbol);
                    }
                    _attributes.Clear();
                }
            }

            if (token.Lexeme == ":")
            {
                if (_insideProduction)
                {
                    var production = new Production(_currentProducer, _currentProduct.ToArray());
                    if (_currentSemanticMethod != null)
                    {
                        _semanticMethods.Add(production, _currentSemanticMethod);
                        _currentSemanticMethod = null;
                    }
                    Grammar.Add(_currentProducer, production);
                    _currentProduct.Clear();
                }
                _insideProduction = !_insideProduction;
            }
            else
            {
                if (_insideProduction == false)
                {
                    _currentProducer = SymbolTable[newToken.Lexeme];
                    _currentProduct = new List<Symbol>();
                }
                else
                {
                    if (token.Lexeme == "|")
                    {
                        var production = new Production(_currentProducer, _currentProduct.ToArray());
                        if (_currentSemanticMethod != null)
                        {
                            _semanticMethods.Add(production, _currentSemanticMethod);
                            _currentSemanticMethod = null;
                        }
                        Grammar.Add(_currentProducer, production);
                        _currentProduct.Clear();
                    }
                    else
                    {
                        _currentProduct.Add(SymbolTable[newToken.Lexeme]);
                    }
                }
            }
        }

        public void DefineTokens()
        {
            _nameBySymbol = SymbolTable.ToDictionary(i => i.Value, i => i.Key);
            for (var i = 0; i < TokenNames.Count; i++)
            {
                var tokenName = TokenNames[i].Replace(@"\", "").Replace(@"'", "");
                if (SymbolTable.ContainsKey(tokenName))
                {
                    TokenDictionary.Add(i, SymbolTable[tokenName].Id);
                }
            }
        }

        public IEnumerable<KeyValuePair<Symbol, Symbol>> SymbolsByAttributeName(string name)
        {
            if (name == null || _symbolsByAttributes.ContainsKey(new Attribute(name)) == false)
                throw new ArgumentException();
            return _symbolsByAttributes[new Attribute(name)].AsEnumerable();
        }

        public Attribute AttributeBySymbolAndName(Symbol symbol, string name)
        {
            if (name == null || _symbolsByAttributes.ContainsKey(new Attribute(name)) == false)
                throw new ArgumentException();
            return _symbolsByAttributes[new Attribute(name)][symbol].Properties[name];
        }

        public string GetSymbolName(Symbol symbol)
        {
            return _nameBySymbol[symbol];
        }

        public Symbol GetSymbolFromName(string name)
        {
            return SymbolTable[name];
        }
    }
}
