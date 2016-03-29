using System;
using System.Collections.Generic;
using System.Linq;
using Token_Analizer;
using Semantic;
using Syntax.Data;
using Syntax.Dynamic;
using Attribute = Semantic.Attribute;

namespace Syntax
{
    public class StringGrammar
    {
        public Grammar Grammar { get; private set; }
        public readonly Dictionary<string, Symbol> SymbolTable;
        private readonly Dictionary<Attribute, Dictionary<Symbol, Symbol>> symbolsByAttributes;
        private Dictionary<Symbol, string> nameBySymbol;
        public Dictionary<int, int> TokenDictionary { get; private set; }
        public List<string> TokenNames { get; private set; }

        private bool insideProduction;
        private Symbol currentProducer;
        private List<Symbol> currentProduct;
        private readonly List<Attribute> attributes;
        private readonly DynamicCodeProvider dynamicCode;
        private readonly Dictionary<Production, string> semanticMethods;
        private string currentSemanticMethod = null;

        public StringGrammar(IEnumerable<string> tokenNames, DynamicCodeProvider dynamicCode, Dictionary<Production, string> semanticMethods)
        {
            Grammar = new Grammar();
            SymbolTable = new Dictionary<string, Symbol>();
            TokenNames = tokenNames.ToList();
            TokenDictionary = new Dictionary<int, int>();

            attributes = new List<Attribute>();
            symbolsByAttributes = new Dictionary<Attribute, Dictionary<Symbol, Symbol>>();
            this.dynamicCode = dynamicCode;
            this.semanticMethods = semanticMethods;
        }

        public void AddSymbolDefinition(Token token)
        {
            var newToken = new Token(token.Type, token.Lexeme.Replace(@"\", ""), token.Message);
            if (token.CsharpCode == true)
            {
                currentSemanticMethod = dynamicCode.AddMethod(token.Lexeme);
                return;
            }
            if (token.Lexeme.StartsWith("#"))
            {
                var lexeme = token.Lexeme.Substring(1).Split(":".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                if (lexeme.Length == 1)
                {
                    attributes.Add(new Attribute(lexeme[0]));
                }
                else
                {
                    var parseValue = 0;
                    var parsed = int.TryParse(lexeme[1], out parseValue);
                    if (parsed == true)
                        attributes.Add(new Attribute(lexeme[0], parseValue));
                    else
                        attributes.Add(new Attribute(lexeme[0]));
                }
                
                return;
            }
            if (token.Lexeme != "|" && token.Lexeme != ":" && attributes.Count != 0 && SymbolTable.ContainsKey(newToken.Lexeme) == true)
            {
                foreach (var attribute in attributes)
                {
                    if (symbolsByAttributes.ContainsKey(attribute) == false)
                        symbolsByAttributes[attribute] = new Dictionary<Symbol, Symbol>();
                    symbolsByAttributes[attribute].Add(SymbolTable[newToken.Lexeme], SymbolTable[newToken.Lexeme]);
                    SymbolTable[newToken.Lexeme].Properties.Add(attribute.Name, attribute);
                }
                attributes.Clear();
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
                    var symbol = Grammar.NewSymbol(type, attributes);
                    SymbolTable.Add(newToken.Lexeme, symbol); //add symbol
                    //Console.WriteLine( "{0} : {1}",token.Lexeme, symbol.Id);
                    foreach (var attribute in attributes)
                    {
                        if (symbolsByAttributes.ContainsKey(attribute) == false)
                            symbolsByAttributes[attribute] = new Dictionary<Symbol, Symbol>();
                        symbolsByAttributes[attribute].Add(symbol, symbol);
                    }
                    attributes.Clear();
                }
            }

            if (token.Lexeme == ":")
            {
                if (insideProduction)
                {
                    var production = new Production(currentProducer, currentProduct.ToArray());
                    if (currentSemanticMethod != null)
                    {
                        semanticMethods.Add(production, currentSemanticMethod);
                        currentSemanticMethod = null;
                    }
                    Grammar.Add(currentProducer, production);
                    currentProduct.Clear();
                }
                insideProduction = !insideProduction;
            }
            else
            {
                if (insideProduction == false)
                {
                    currentProducer = SymbolTable[newToken.Lexeme];
                    currentProduct = new List<Symbol>();
                }
                else
                {
                    if (token.Lexeme == "|")
                    {
                        var production = new Production(currentProducer, currentProduct.ToArray());
                        if (currentSemanticMethod != null)
                        {
                            semanticMethods.Add(production, currentSemanticMethod);
                            currentSemanticMethod = null;
                        }
                        Grammar.Add(currentProducer, production);
                        currentProduct.Clear();
                    }
                    else
                    {
                        currentProduct.Add(SymbolTable[newToken.Lexeme]);
                    }
                }
            }
        }

        public void DefineTokens()
        {
            nameBySymbol = SymbolTable.ToDictionary(i => i.Value, i => i.Key);
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
            if (name == null || symbolsByAttributes.ContainsKey(new Attribute(name)) == false)
                throw new ArgumentException();
            return symbolsByAttributes[new Attribute(name)].AsEnumerable();
        }

        public Attribute AttributeBySymbolAndName(Symbol symbol, string name)
        {
            if (name == null || symbolsByAttributes.ContainsKey(new Attribute(name)) == false)
                throw new ArgumentException();
            return symbolsByAttributes[new Attribute(name)][symbol].Properties[name];
        }

        public string GetSymbolName(Symbol symbol)
        {
            return nameBySymbol[symbol];
        }

        public Symbol GetSymbolFromName(string name)
        {
            return SymbolTable[name];
        }
    }
}
