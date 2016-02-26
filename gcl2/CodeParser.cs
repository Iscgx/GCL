using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Token_Analizer;

namespace gcl2
{
    public delegate void OnLexicalError(string message);

    public class CodeParser
    {
        private readonly StringGrammar _stringGrammar;
        private readonly Parser _parser;
        private readonly Stack<int> _nodeStack;
        private readonly Stack<Symbol> _temporalStack;
        private bool error = false;
        public OnLexicalError OnLexicalError;

        private Dictionary<int, int> _u; 
        public CodeParser(string sourceFile)
        {
            var GrammarTokens = System.IO.File.ReadAllText("GrammarTokens.txt");
            var Tokens = System.IO.File.ReadAllText("Tokens.txt");
            var readGrammarLexer = new Lexer(GrammarTokens);
            var lexer = new Lexer(Tokens);
            _stringGrammar = new StringGrammar(lexer.TokenNames);
            _nodeStack = new Stack<int>();
            _nodeStack.Push(0);
            _temporalStack = new Stack<Symbol>();
            readGrammarLexer.TokenCourier += _stringGrammar.AddSymbolDefinition;
            var GrammarGCL = System.IO.File.ReadAllText("GrammarGCL.txt");
            readGrammarLexer.Start(GrammarGCL);
            _stringGrammar.DefineTokens();
            _parser = new Parser(_stringGrammar.Grammar, new Symbol(SymbolType.NonTerminal, 1));
            var then = DateTime.Now;
            lexer.TokenCourier += Parse;
            OnLexicalError += Console.WriteLine;
            _u = _stringGrammar.TokenDictionary.ToDictionary(i => i.Value, i => i.Key);
            var SourceCode = System.IO.File.ReadAllText("SourceCode.txt");
            lexer.Start(SourceCode);
            
            //DEBUG
            
            Console.WriteLine((DateTime.Now - then).TotalMilliseconds + @" ms");
        }
        
        public void Parse(Token token)
        {
            if (_stringGrammar.TokenDictionary.ContainsKey(token.Type) == false)
            {
                if (OnLexicalError != null)
                    OnLexicalError(token.Message);
            }
            else
            {
 
                var type = _stringGrammar.TokenDictionary[token.Type] == -1 ? SymbolType.EndOfFile : SymbolType.Terminal;
                var symbol = new Symbol(type, _stringGrammar.TokenDictionary[token.Type]);
                var action = _parser.SyntaxTable[_nodeStack.Peek(), symbol];
                switch (action.Item1)
                {
                    case ActionType.Shift:
                        Shift(action.Item2, symbol);
                        break;
                    case ActionType.Accept:
                        if (error == true)
                            Console.WriteLine(@"Not Accepted");
                        else
                            Console.WriteLine(@"Accepted");
                        
                        break;
                    case ActionType.Reduce:
                        Reduce(action.Item2);
                        Parse(token);
                        
                        break;
                    case ActionType.Error:
                        Console.WriteLine(@"Syntax error at line {0}, token {1}.", token.Message, token.Lexeme);
                        error = true;
                        break;
                    case ActionType.GoTo:
                        break;
                }
            }
        }

        private void Shift(int value, Symbol symbol)
        {
            _nodeStack.Push(value);
            _temporalStack.Push(symbol);
        }

        private void Reduce(int value)
        {
            var production = _parser.SyntaxTable.ProductionById(value);
            PrintProduction(production, _stringGrammar.TokenNames);
            for (var i = 0; i < production.Product.Count; i++)
            {
                _nodeStack.Pop();
                _temporalStack.Pop();
            }
            _temporalStack.Push(production.Producer);
            var goTo = _parser.SyntaxTable[_nodeStack.Peek(), production.Producer];
            _nodeStack.Push(goTo.Item2);

        }

        private void PrintProduction(Production prod, IList<string> tokenNames)
        {
            var builder = new StringBuilder();

            builder.Append(string.Format("[{0}] -> ", _stringGrammar.GetSymbolName(prod.Producer)));
            foreach (var symbol in prod.Product)
            {
                builder.Append(string.Format("[{0}]", _stringGrammar.GetSymbolName(symbol)));
            }
            Console.WriteLine(builder.ToString());
        }
    }
}
