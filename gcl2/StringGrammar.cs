using System.Collections.Generic;
using System.Linq;
using Token_Analizer;

namespace gcl2
{
    public class StringGrammar
    {
        public Grammar Grammar { get; private set; }
        private readonly Dictionary<string, Symbol> _symbolTable;
        private Dictionary<Symbol, string> _nameBySymbol;
        public Dictionary<int, int> TokenDictionary { get; private set; }
        public List<string> TokenNames { get; private set; }

        private bool _insideProduction = false;
        private Symbol _currentProducer;
        private List<Symbol> _currentProduct;

        public StringGrammar(IEnumerable<string> tokenNames)
        {
            Grammar = new Grammar();
            _symbolTable = new Dictionary<string, Symbol>();
            TokenNames = tokenNames.ToList();
            TokenDictionary = new Dictionary<int, int>();
        }

        public void AddSymbolDefinition(Token token)
        {
            var newToken = new Token(token.Type, token.Lexeme.Replace(@"\", ""), token.Message);
            if (token.PythonCode)
            {
                //Do corresponding task for semantic actions
                return;
            }
            if (token.Lexeme != "|" && token.Lexeme != ":" && _symbolTable.ContainsKey(newToken.Lexeme) == false)
            {
                if (token.Lexeme == "endOfFile")
                {
                    _symbolTable.Add("endOfFile", Grammar.EndOfFile);
                }
                else
                {
                    var type = SymbolType.Terminal;
                    if (char.IsUpper(newToken.Lexeme.First()))
                        type = SymbolType.NonTerminal;
                    _symbolTable.Add(newToken.Lexeme, Grammar.NewSymbol(type));
                    //System.Console.WriteLine("{0} : {1}", newToken.Lexeme, _symbolTable[newToken.Lexeme].Id);
                }
            }

            if (token.Lexeme == ":")
            {
                if (_insideProduction == true)
                {
                    var production = new Production(_currentProducer, _currentProduct.ToArray());
                    Grammar.Add(_currentProducer, production);
                    _currentProduct.Clear();
                }
                _insideProduction = !_insideProduction;
            }
            else
            {
                if (_insideProduction == false)
                {
                    _currentProducer = _symbolTable[newToken.Lexeme];
                    _currentProduct = new List<Symbol>();
                }
                else
                {
                    if (token.Lexeme == "|")
                    {
                        var production = new Production(_currentProducer, _currentProduct.ToArray());
                        Grammar.Add(_currentProducer, production);
                        _currentProduct.Clear();
                    }
                    else
                    {
                        _currentProduct.Add(_symbolTable[newToken.Lexeme]);
                    }
                }
            }
        }


        public void DefineTokens()
        {
            _nameBySymbol = _symbolTable.ToDictionary(i => i.Value, i => i.Key);
            for (var i = 0; i < TokenNames.Count; i++)
            {
                var tokenName = TokenNames[i].Replace(@"\", "").Replace(@"'", "");
                if (_symbolTable.ContainsKey(tokenName) == true)
                {
                    TokenDictionary.Add(i, _symbolTable[tokenName].Id);
                }
            }
        }

        public string GetSymbolName(Symbol symbol)
        {
            return _nameBySymbol[symbol];
        }

    }
}
