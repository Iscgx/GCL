using System;
using System.Collections.Generic;
using System.Linq;
using GCL.Lex;
using GCL.Syntax.Data;
using GCL.Syntax.Dynamic;
using Semantic;
using Attribute = Semantic.Attribute;

namespace GCL.Syntax
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
            this.SymbolTable = new Dictionary<string, Symbol>();
            TokenNames = tokenNames.ToList();
            TokenDictionary = new Dictionary<int, int>();

            this.attributes = new List<Attribute>();
            this.symbolsByAttributes = new Dictionary<Attribute, Dictionary<Symbol, Symbol>>();
            this.dynamicCode = dynamicCode;
            this.semanticMethods = semanticMethods;
        }

        public void AddSymbolDefinition(Token token)
        {
            var newToken = new Token(token.Type, token.Lexeme.Replace(@"\", ""), token.Message);
            if (token.CsharpCode == true)
            {
                this.currentSemanticMethod = this.dynamicCode.AddMethod(token.Lexeme);
                return;
            }
            if (token.Lexeme.StartsWith("#"))
            {
                var lexeme = token.Lexeme.Substring(1).Split(":".ToArray(), StringSplitOptions.RemoveEmptyEntries);
                if (lexeme.Length == 1)
                {
                    this.attributes.Add(new Attribute(lexeme[0]));
                }
                else
                {
                    var parseValue = 0;
                    var parsed = int.TryParse(lexeme[1], out parseValue);
                    if (parsed == true)
                        this.attributes.Add(new Attribute(lexeme[0], parseValue));
                    else
                        this.attributes.Add(new Attribute(lexeme[0]));
                }
                
                return;
            }
            if (token.Lexeme != "|" && token.Lexeme != ":" && this.attributes.Count != 0 && this.SymbolTable.ContainsKey(newToken.Lexeme) == true)
            {
                foreach (var attribute in this.attributes)
                {
                    if (this.symbolsByAttributes.ContainsKey(attribute) == false)
                        this.symbolsByAttributes[attribute] = new Dictionary<Symbol, Symbol>();
                    this.symbolsByAttributes[attribute].Add(this.SymbolTable[newToken.Lexeme], this.SymbolTable[newToken.Lexeme]);
                    this.SymbolTable[newToken.Lexeme].Properties.Add(attribute.Name, attribute);
                }
                this.attributes.Clear();
            }
            if (token.Lexeme != "|" && token.Lexeme != ":" && this.SymbolTable.ContainsKey(newToken.Lexeme) == false)
            {
                if (token.Lexeme == "endOfFile")
                {
                    this.SymbolTable.Add("endOfFile", Grammar.EndOfFile);
                }
                else
                {
                    var type = SymbolType.Terminal;
                    if (char.IsUpper(newToken.Lexeme.First()))
                        type = SymbolType.NonTerminal;
                    var symbol = Grammar.NewSymbol(type, this.attributes);
                    this.SymbolTable.Add(newToken.Lexeme, symbol); //add symbol
                    //Console.WriteLine( "{0} : {1}",token.Lexeme, symbol.Id);
                    foreach (var attribute in this.attributes)
                    {
                        if (this.symbolsByAttributes.ContainsKey(attribute) == false)
                            this.symbolsByAttributes[attribute] = new Dictionary<Symbol, Symbol>();
                        this.symbolsByAttributes[attribute].Add(symbol, symbol);
                    }
                    this.attributes.Clear();
                }
            }

            if (token.Lexeme == ":")
            {
                if (this.insideProduction)
                {
                    var production = new Production(this.currentProducer, this.currentProduct.ToArray());
                    if (this.currentSemanticMethod != null)
                    {
                        this.semanticMethods.Add(production, this.currentSemanticMethod);
                        this.currentSemanticMethod = null;
                    }
                    Grammar.Add(this.currentProducer, production);
                    this.currentProduct.Clear();
                }
                this.insideProduction = !this.insideProduction;
            }
            else
            {
                if (this.insideProduction == false)
                {
                    this.currentProducer = this.SymbolTable[newToken.Lexeme];
                    this.currentProduct = new List<Symbol>();
                }
                else
                {
                    if (token.Lexeme == "|")
                    {
                        var production = new Production(this.currentProducer, this.currentProduct.ToArray());
                        if (this.currentSemanticMethod != null)
                        {
                            this.semanticMethods.Add(production, this.currentSemanticMethod);
                            this.currentSemanticMethod = null;
                        }
                        Grammar.Add(this.currentProducer, production);
                        this.currentProduct.Clear();
                    }
                    else
                    {
                        this.currentProduct.Add(this.SymbolTable[newToken.Lexeme]);
                    }
                }
            }
        }

        public void DefineTokens()
        {
            this.nameBySymbol = this.SymbolTable.ToDictionary(i => i.Value, i => i.Key);
            for (var i = 0; i < TokenNames.Count; i++)
            {
                var tokenName = TokenNames[i].Replace(@"\", "").Replace(@"'", "");
                if (this.SymbolTable.ContainsKey(tokenName))
                {
                    TokenDictionary.Add(i, this.SymbolTable[tokenName].Id);
                }
            }
        }

        public IEnumerable<KeyValuePair<Symbol, Symbol>> SymbolsByAttributeName(string name)
        {
            if (name == null || this.symbolsByAttributes.ContainsKey(new Attribute(name)) == false)
                throw new ArgumentException();
            return this.symbolsByAttributes[new Attribute(name)].AsEnumerable();
        }

        public Attribute AttributeBySymbolAndName(Symbol symbol, string name)
        {
            if (name == null || this.symbolsByAttributes.ContainsKey(new Attribute(name)) == false)
                throw new ArgumentException();
            return this.symbolsByAttributes[new Attribute(name)][symbol].Properties[name];
        }

        public string GetSymbolName(Symbol symbol)
        {
            return this.nameBySymbol[symbol];
        }

        public Symbol GetSymbolFromName(string name)
        {
            return this.SymbolTable[name];
        }
    }
}
