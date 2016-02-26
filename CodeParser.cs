using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Token_Analizer;
using gcl2.Data;
using gcl2.Dynamic;
using Semantic;

namespace gcl2
{
    public delegate void OnLexicalError(string message);

    public delegate void OnSintacticalError(string message);

    public class CodeParser
    {
        private readonly StringGrammar _stringGrammar;
        private readonly Parser _parser;
        private readonly Stack<int> _nodeStack;
        private readonly Stack<Symbol> _temporalStack;
        private readonly List<Symbol> _productionSymbols;
        private readonly Lexer _lexer;
        private readonly Dictionary<Production, string> _semanticMethods;
        private readonly CompiledClass _compiledSemanticMethods;
        private readonly SemanticAnalysis _semantic;
        private readonly GclCodeGenerator _codeGenerator;
        private readonly BoolWrapper _atDevice;
        private readonly BoolWrapper _cudaDefined;

        private bool _started = false;
        private DateTime _parseStartTime;
        private bool _accepted = true;
        private bool _onErrorRecoveryMode;
        private int _errorStateS;

        public OnLexicalError OnLexicalError;
        public OnSintacticalError OnSintacticalError;

        public CodeParser(string tokensCode, string grammarTokensCode, string codeGrammar)
        {
            var then = DateTime.Now;
            _semanticMethods = new Dictionary<Production, string>();
            var dynamicCode = new DynamicCodeProvider();
            _semantic = new SemanticAnalysis();
            
            _productionSymbols = new List<Symbol>();
            dynamicCode.AddToScope(_semantic, "semantic");
            dynamicCode.AddToScope(_productionSymbols, "element");
            dynamicCode.AddToScope(_semantic.ThrowError, "ThrowError");
            _atDevice = new BoolWrapper(false);
            _cudaDefined = new BoolWrapper(false);
            dynamicCode.AddToScope(_atDevice, "AtDevice");
            dynamicCode.AddToScope(_cudaDefined, "CudaDefined");
            var readGrammarLexer = new Lexer(grammarTokensCode);
            _lexer = new Lexer(tokensCode);
            _stringGrammar = new StringGrammar(_lexer.TokenNames, dynamicCode, _semanticMethods);
            _nodeStack = new Stack<int>();
            _nodeStack.Push(0);
            _temporalStack = new Stack<Symbol>();
            readGrammarLexer.TokenCourier += _stringGrammar.AddSymbolDefinition;

            readGrammarLexer.Start(codeGrammar);
            _stringGrammar.DefineTokens();
            _parser = new Parser(_stringGrammar.Grammar, new Symbol(SymbolType.NonTerminal, 1));
            _lexer.TokenCourier += ParseToken;

            _codeGenerator = new GclCodeGenerator(10000);
            dynamicCode.AddToScope(_codeGenerator, "codegen");

            //File.WriteAllText(@"D:\code.txt",dynamicCode.GetCsCode());
            try
            {
                _compiledSemanticMethods = CsCodeCompiler.Compile(dynamicCode, "Semantic.dll", "Microsoft.CSharp.dll", "System.Core.dll", "System.dll", "System.Collections.dll");
            }
            catch (Exception)
            {

            }
            Console.WriteLine(@"Init: {0} ms", (DateTime.Now - then).TotalMilliseconds);
        }
        
        public void Parse(string code)
        {
            _nodeStack.Clear();
            _nodeStack.Push(0);
            _temporalStack.Clear();
            _lexer.Start(code);
            _codeGenerator.End();
        }

        public void ParseToken(Token token)
        {
            if (_started == false)
            {
                _parseStartTime = DateTime.Now;
                _started = true;
            }
            if (_stringGrammar.TokenDictionary.ContainsKey(token.Type) == false)
            {
                if (OnLexicalError != null)
                    OnLexicalError(token.Message);
            }
            else
            {
                var type = _stringGrammar.TokenDictionary[token.Type] == -1 ? SymbolType.EndOfFile : SymbolType.Terminal;
                var symbol = new Symbol(type, _stringGrammar.TokenDictionary[token.Type]);
                if(symbol.Type == SymbolType.Terminal)
                {
                    symbol.Attributes.Lexeme = token.Lexeme;
                    symbol.Attributes.LineNumber = token.Message;
                }

                if (_onErrorRecoveryMode && KeepEatingTokens(token))
                {
                    return;
                }
                //else
                //{
                var action = _parser.SyntaxTable[_nodeStack.Peek(), symbol];
                switch (action.Item1)
                {
                    case ActionType.Shift:
                        Shift(action.Item2, symbol);
                        break;
                    case ActionType.Accept:
                        Console.WriteLine(_accepted && _semantic.SemanticError == false ? @"Accepted" : @"Not accepted");
                        Console.WriteLine(@"Parse: {0} ms", (DateTime.Now - _parseStartTime).TotalMilliseconds);
                        break;
                    case ActionType.Reduce:
                        Reduce(action.Item2);
                        ParseToken(token);
                        break;
                    case ActionType.Error:
                        if (OnSintacticalError != null)
                            OnSintacticalError(string.Format(@"Syntax error at line {0}, token {1}.", token.Message, token.Lexeme));
                        Console.WriteLine(@"Syntax error at line {0}, near token {1}.", token.Message, token.Lexeme);
                        Console.Write(@"Expecting token:");
                        var first = true;
                        foreach(var sym in _stringGrammar.SymbolTable)
                        {
                            if (sym.Value.Type == SymbolType.Terminal && _parser.SyntaxTable.ContainsKey(_nodeStack.Peek(), sym.Value))
                            {
                                if (first)
                                    first = false;
                                else
                                    Console.Write("|");
                                Console.Write(" \"{0}\" ", sym.Key);
                            }
                        }
                        Console.WriteLine("\n");

                        _accepted = false;

                        PanicModeErrorRecovery();
                        break;
                    case ActionType.GoTo:
                        break;
                }
                //}  
            }
        }

        private bool KeepEatingTokens(Token token)
        {
            //discard zero or more input symbols until a symbol a is found that can legitimately follow A.
            var type = _stringGrammar.TokenDictionary[token.Type] == -1 ? SymbolType.EndOfFile : SymbolType.Terminal;
            var symbol = new Symbol(type, _stringGrammar.TokenDictionary[token.Type]);
            bool keepEatingTokens = !(_parser.SyntaxTable.ContainsKey(_errorStateS, symbol));

            if (!keepEatingTokens)
            {
                _onErrorRecoveryMode = false;
                //The parser then shifts the state goto [S, A] on the stack and resumes normal parsing.
                if (_parser != null)
                    if (_parser.SyntaxTable != null)
                        _nodeStack.Push(_errorStateS);
            }

            return keepEatingTokens;
        }

        private void PanicModeErrorRecovery()
        {

            /*
             * A systematic method for error recovery in LR parsing is to scan down the stack until a state S with a goto on a particular nonterminal A is found,
             * and then discard zero or more input symbols until a symbol a is found that can legitimately follow A. The parser then shifts the state goto [S, A]
             * on the stack and resumes normal parsing.
             */

            //Retrieve set of symbols with property block
            var aSymbols = _stringGrammar.SymbolsByAttributeName("block");

            int s;
            var A=new Symbol(SymbolType.NonTerminal, 0);
            var aIsNotDefined = true;
            var aPriority=0;

            //scan down the stack until a state S with a goto on a particular nonterminal A is found
            do
            {
                s = _nodeStack.Pop();// ElementAt(count++);
                foreach (var aSymbol in aSymbols)
                {
                    if (_parser.SyntaxTable.ContainsKey(s, aSymbol.Key))
                    {
                        if (aIsNotDefined)
                        {
                            A = aSymbol.Key;
                            aPriority = _stringGrammar.AttributeBySymbolAndName(aSymbol.Key, "priority").Value.Value;

                            aIsNotDefined = false;
                        }
                        else
                        {
                            //Checking Asymbol priority: if greater than, reassign Asymbol to A
                            var aSymbolPriority = _stringGrammar.AttributeBySymbolAndName(aSymbol.Key, "priority").Value.Value;
                            if (aSymbolPriority > aPriority)
                            {
                                aPriority = aSymbolPriority;
                                A = aSymbol.Key;
                            }
                        }
                    }

                }
            } while (aIsNotDefined);


            _errorStateS = s;
            _onErrorRecoveryMode = true;
        }

        private void Shift(int value, Symbol symbol)
        {
            _nodeStack.Push(value);
            var s = (Symbol) symbol.Clone();
            s.Attributes.Lexeme = symbol.Attributes.Lexeme;
            _temporalStack.Push(s);
        }

        private void Reduce(int value)
        {
            var production = _parser.SyntaxTable.ProductionById(value);

            
            var reversedStack = new Stack<Symbol>();
            for (var i = 0; i < production.Product.Count; i++)
            {
                _nodeStack.Pop();
                reversedStack.Push(_temporalStack.Pop());
            }
            var producer = (Symbol) production.Producer.Clone();
            _productionSymbols.Add(producer);
            foreach (var symbol in reversedStack)
            {
                _productionSymbols.Add(symbol);
            }

            if (_compiledSemanticMethods != null && _semanticMethods.ContainsKey(production) == true)
                _compiledSemanticMethods.Call(_semanticMethods[production]);
            //PrintProduction(production);
            _productionSymbols.Clear();
            _temporalStack.Push(producer);
            var goTo = _parser.SyntaxTable[_nodeStack.Peek(), production.Producer];
            _nodeStack.Push(goTo.Item2);

        }

        private void PrintProduction(Production prod)
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
