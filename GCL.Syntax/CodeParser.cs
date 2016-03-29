using System;
using System.Collections.Generic;
using System.Text;
using GCL.Lex;
using GCL.Syntax.Data;
using GCL.Syntax.Dynamic;
using Semantic;

namespace GCL.Syntax
{
    public delegate void OnLexicalError(string message);

    public delegate void OnSintacticalError(string message);

    public class CodeParser
    {
        private readonly StringGrammar stringGrammar;
        private readonly Parser parser;
        private readonly Stack<int> nodeStack;
        private readonly Stack<Symbol> temporalStack;
        private readonly List<Symbol> productionSymbols;
        private readonly ILexer lexer;
        private readonly Dictionary<Production, string> semanticMethods;
        private readonly CompiledClass compiledSemanticMethods;
        private readonly SemanticAnalysis semanticAnalysis;
        private readonly GclCodeGenerator codeGenerator;
        private readonly BoolWrapper atDevice;
        private readonly BoolWrapper cudaDefined;

        private bool started = false;
        private DateTime parseStartTime;
        private bool accepted = true;
        private bool onErrorRecoveryMode;
        private int errorStateS;

        public OnLexicalError OnLexicalError;
        public OnSintacticalError OnSintacticalError;

        public CodeParser(
            ILexer codeLexer,
            GclCodeGenerator gclCodeGenerator,
            DynamicCodeProvider dynamicCodeProvider,
            SemanticAnalysis semanticAnalysis,
            IEnumerable<Token> grammarTokens)
        {
            var then = DateTime.Now;
            semanticMethods = new Dictionary<Production, string>();
            nodeStack = new Stack<int>();
            nodeStack.Push(0);
            temporalStack = new Stack<Symbol>();
            productionSymbols = new List<Symbol>();
            atDevice = new BoolWrapper(false);
            cudaDefined = new BoolWrapper(false);
            this.semanticAnalysis = semanticAnalysis;
            this.lexer = codeLexer;
            stringGrammar = new StringGrammar(this.lexer.TokenNames, dynamicCodeProvider, semanticMethods);

            foreach (var token in grammarTokens)
            {
                stringGrammar.AddSymbolDefinition(token);
            }

            stringGrammar.DefineTokens();
            parser = new Parser(stringGrammar.Grammar, new Symbol(SymbolType.NonTerminal, 1));

            codeGenerator = gclCodeGenerator;
            dynamicCodeProvider.AddToScope(codeGenerator, "codegen");
            dynamicCodeProvider.AddToScope(this.semanticAnalysis, "semantic");
            dynamicCodeProvider.AddToScope(productionSymbols, "element");
            dynamicCodeProvider.AddToScope(this.semanticAnalysis.ThrowError, "ThrowError");
            dynamicCodeProvider.AddToScope(atDevice, "AtDevice");
            dynamicCodeProvider.AddToScope(cudaDefined, "CudaDefined");

            //File.WriteAllText(@"D:\code.txt",dynamicCode.GetCsCode());
            try
            {
                compiledSemanticMethods = CsCodeCompiler.Compile(dynamicCodeProvider,
                    "Semantic.dll",
                    "Microsoft.CSharp.dll",
                    "System.Core.dll",
                    "System.dll",
                    "System.Collections.dll");
            }
            catch (Exception)
            {

            }
            Console.WriteLine(@"Init: {0} ms", (DateTime.Now - then).TotalMilliseconds);
        }

        public string Parse(string code)
        {
            nodeStack.Clear();
            nodeStack.Push(0);
            temporalStack.Clear();
            foreach (var token in lexer.Parse(code))
            {
                ParseToken(token);
            }
            return codeGenerator.End();
        }

        public void ParseToken(Token token)
        {
            if (started == false)
            {
                parseStartTime = DateTime.Now;
                started = true;
            }
            if (stringGrammar.TokenDictionary.ContainsKey(token.Type) == false)
            {
                if (OnLexicalError != null)
                    OnLexicalError(token.Message);
            }
            else
            {
                var type = stringGrammar.TokenDictionary[token.Type] == -1 ? SymbolType.EndOfFile : SymbolType.Terminal;
                var symbol = new Symbol(type, stringGrammar.TokenDictionary[token.Type]);
                if(symbol.Type == SymbolType.Terminal)
                {
                    symbol.Attributes.Lexeme = token.Lexeme;
                    symbol.Attributes.LineNumber = token.Message;
                }

                if (onErrorRecoveryMode && KeepEatingTokens(token))
                {
                    return;
                }
                //else
                //{
                var action = parser.SyntaxTable[nodeStack.Peek(), symbol];
                switch (action.Item1)
                {
                    case ActionType.Shift:
                        Shift(action.Item2, symbol);
                        break;
                    case ActionType.Accept:
                        Console.WriteLine(accepted && semanticAnalysis.SemanticError == false ? @"Accepted" : @"Not accepted");
                        Console.WriteLine(@"Parse: {0} ms", (DateTime.Now - parseStartTime).TotalMilliseconds);
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
                        foreach(var sym in stringGrammar.SymbolTable)
                        {
                            if (sym.Value.Type == SymbolType.Terminal && parser.SyntaxTable.ContainsKey(nodeStack.Peek(), sym.Value))
                            {
                                if (first)
                                    first = false;
                                else
                                    Console.Write("|");
                                Console.Write(" \"{0}\" ", sym.Key);
                            }
                        }
                        Console.WriteLine("\n");

                        accepted = false;

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
            var type = stringGrammar.TokenDictionary[token.Type] == -1 ? SymbolType.EndOfFile : SymbolType.Terminal;
            var symbol = new Symbol(type, stringGrammar.TokenDictionary[token.Type]);
            bool keepEatingTokens = !(parser.SyntaxTable.ContainsKey(errorStateS, symbol));

            if (!keepEatingTokens)
            {
                onErrorRecoveryMode = false;
                //The parser then shifts the state goto [S, A] on the stack and resumes normal parsing.
                if (parser != null)
                    if (parser.SyntaxTable != null)
                        nodeStack.Push(errorStateS);
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
            var aSymbols = stringGrammar.SymbolsByAttributeName("block");

            int s;
            var a=new Symbol(SymbolType.NonTerminal, 0);
            var aIsNotDefined = true;
            var aPriority=0;

            //scan down the stack until a state S with a goto on a particular nonterminal A is found
            do
            {
                s = nodeStack.Pop();// ElementAt(count++);
                foreach (var aSymbol in aSymbols)
                {
                    if (parser.SyntaxTable.ContainsKey(s, aSymbol.Key))
                    {
                        if (aIsNotDefined)
                        {
                            a = aSymbol.Key;
                            aPriority = stringGrammar.AttributeBySymbolAndName(aSymbol.Key, "priority").Value.Value;

                            aIsNotDefined = false;
                        }
                        else
                        {
                            //Checking Asymbol priority: if greater than, reassign Asymbol to A
                            var aSymbolPriority = stringGrammar.AttributeBySymbolAndName(aSymbol.Key, "priority").Value.Value;
                            if (aSymbolPriority > aPriority)
                            {
                                aPriority = aSymbolPriority;
                                a = aSymbol.Key;
                            }
                        }
                    }

                }
            } while (aIsNotDefined);


            errorStateS = s;
            onErrorRecoveryMode = true;
        }

        private void Shift(int value, Symbol symbol)
        {
            nodeStack.Push(value);
            var s = (Symbol) symbol.Clone();
            s.Attributes.Lexeme = symbol.Attributes.Lexeme;
            temporalStack.Push(s);
        }

        private void Reduce(int value)
        {
            var production = parser.SyntaxTable.ProductionById(value);

            
            var reversedStack = new Stack<Symbol>();
            for (var i = 0; i < production.Product.Count; i++)
            {
                nodeStack.Pop();
                reversedStack.Push(temporalStack.Pop());
            }
            var producer = (Symbol) production.Producer.Clone();
            productionSymbols.Add(producer);
            foreach (var symbol in reversedStack)
            {
                productionSymbols.Add(symbol);
            }

            if (compiledSemanticMethods != null && semanticMethods.ContainsKey(production) == true)
                compiledSemanticMethods.Call(semanticMethods[production]);
            //PrintProduction(production);
            productionSymbols.Clear();
            temporalStack.Push(producer);
            var goTo = parser.SyntaxTable[nodeStack.Peek(), production.Producer];
            nodeStack.Push(goTo.Item2);

        }

        private void PrintProduction(Production prod)
        {
            var builder = new StringBuilder();

            builder.Append(string.Format("[{0}] -> ", stringGrammar.GetSymbolName(prod.Producer)));
            foreach (var symbol in prod.Product)
            {
                builder.Append(string.Format("[{0}]", stringGrammar.GetSymbolName(symbol)));
            }
            Console.WriteLine(builder.ToString());
        }
    }
}
