using System;
using System.Collections.Generic;
using System.Text;
using GCL.Lex;
using GCL.Syntax.Data;
using GCL.Syntax.Dynamic;
using Semantic;

namespace GCL.Syntax
{
    public class CodeParserWithSupportForCodeGeneration
    {
        private readonly StringGrammar stringGrammar;
        private readonly Parser parser;
        private readonly Stack<int> nodeStack;
        private readonly Stack<Symbol> temporalStack;
        private readonly List<Symbol> productionSymbols;
        private readonly Dictionary<Production, string> semanticMethods;
        private readonly CompiledClass compiledSemanticMethods;
        private readonly SemanticAnalysis semanticAnalysis;
        private readonly GclCodeGenerator gclCodeGenerator;
        private readonly BoolWrapper atDevice;
        private readonly BoolWrapper cudaDefined;

        private bool started = false;
        private DateTime parseStartTime;
        private bool accepted = true;
        private bool onErrorRecoveryMode;
        private int errorStateS;


        public CodeParserWithSupportForCodeGeneration(GclCodeGenerator gclCodeGenerator,
            DynamicCodeProvider dynamicCodeProvider,
            SemanticAnalysis semanticAnalysis,
            Dictionary<Production, string> semanticMethods,
            StringGrammar stringGrammar,
            Parser parser)
        {
            var then = DateTime.Now;
            this.semanticMethods = semanticMethods;
            this.nodeStack = new Stack<int>();
            this.nodeStack.Push(0);
            this.temporalStack = new Stack<Symbol>();
            this.productionSymbols = new List<Symbol>();
            this.atDevice = new BoolWrapper(false);
            this.cudaDefined = new BoolWrapper(false);
            this.semanticAnalysis = semanticAnalysis;
            this.stringGrammar = stringGrammar;
            
            this.parser = parser;

            this.gclCodeGenerator = gclCodeGenerator;

            dynamicCodeProvider.AddToScope(this.productionSymbols, "element");
            dynamicCodeProvider.AddToScope(this.atDevice, "AtDevice");
            dynamicCodeProvider.AddToScope(this.cudaDefined, "CudaDefined");

            //File.WriteAllText(@"D:\code.txt",dynamicCode.GetCsCode());
            try
            {
                this.compiledSemanticMethods = CsCodeCompiler.Compile(dynamicCodeProvider,
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

        public Dictionary<ActionType, int> Parse(IEnumerable<Token> tokens)
        {
            this.nodeStack.Clear();
            this.nodeStack.Push(0);
            this.temporalStack.Clear();

            var actionCounts = new Dictionary<ActionType, int>();
            foreach (var token in tokens)
            {
                ParseToken(token, actionCounts);
            }
            return actionCounts;
        }

        public void ParseToken(Token token,
            Dictionary<ActionType, int> actionCounts)
        {
            if (this.started == false)
            {
                this.parseStartTime = DateTime.Now;
                this.started = true;
            }
            if (this.stringGrammar.TokenDictionary.ContainsKey(token.Type) == false)
            {
            }
            else
            {
                var type = this.stringGrammar.TokenDictionary[token.Type] == -1 ? SymbolType.EndOfFile : SymbolType.Terminal;
                var symbol = new Symbol(type, this.stringGrammar.TokenDictionary[token.Type]);
                if(symbol.Type == SymbolType.Terminal)
                {
                    symbol.Attributes.Lexeme = token.Lexeme;
                    symbol.Attributes.LineNumber = token.Message;
                }

                if (this.onErrorRecoveryMode && KeepEatingTokens(token))
                {
                    return;
                }
                //else
                //{
                var action = this.parser.SyntaxTable[this.nodeStack.Peek(), symbol];

                if (actionCounts.ContainsKey(action.Item1) == false)
                    actionCounts[action.Item1] = 0;

                actionCounts[action.Item1]++;

                switch (action.Item1)
                {
                    case ActionType.Shift:
                        Shift(action.Item2, symbol);
                        break;
                    case ActionType.Accept:
                        Console.WriteLine(this.accepted && this.semanticAnalysis.SemanticError == false ? @"Accepted" : @"Not accepted");
                        Console.WriteLine(@"Parse: {0} ms", (DateTime.Now - this.parseStartTime).TotalMilliseconds);
                        break;
                    case ActionType.Reduce:
                        Reduce(action.Item2);
                        ParseToken(token, actionCounts);
                        break;
                    case ActionType.Error:
                        Console.WriteLine(@"Syntax error at line {0}, near token {1}.", token.Message, token.Lexeme);
                        Console.Write(@"Expecting token:");
                        var first = true;
                        foreach(var sym in this.stringGrammar.SymbolTable)
                        {
                            if (sym.Value.Type == SymbolType.Terminal && this.parser.SyntaxTable.ContainsKey(this.nodeStack.Peek(), sym.Value))
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    Console.Write("|");
                                }
                                Console.Write(@" ""{0}"" ", sym.Key);
                            }
                        }
                        Console.WriteLine("\n");

                        this.accepted = false;

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
            var type = this.stringGrammar.TokenDictionary[token.Type] == -1 ? SymbolType.EndOfFile : SymbolType.Terminal;
            var symbol = new Symbol(type, this.stringGrammar.TokenDictionary[token.Type]);
            bool keepEatingTokens = !(this.parser.SyntaxTable.ContainsKey(this.errorStateS, symbol));

            if (!keepEatingTokens)
            {
                this.onErrorRecoveryMode = false;
                //The parser then shifts the state goto [S, A] on the stack and resumes normal parsing.
                if (this.parser != null)
                    if (this.parser.SyntaxTable != null)
                        this.nodeStack.Push(this.errorStateS);
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
            var aSymbols = this.stringGrammar.SymbolsByAttributeName("block");

            int s;
            var a=new Symbol(SymbolType.NonTerminal, 0);
            var aIsNotDefined = true;
            var aPriority=0;

            //scan down the stack until a state S with a goto on a particular nonterminal A is found
            do
            {
                s = this.nodeStack.Pop();// ElementAt(count++);
                foreach (var aSymbol in aSymbols)
                {
                    if (this.parser.SyntaxTable.ContainsKey(s, aSymbol.Key))
                    {
                        if (aIsNotDefined)
                        {
                            a = aSymbol.Key;
                            aPriority = this.stringGrammar.AttributeBySymbolAndName(aSymbol.Key, "priority").Value.Value;

                            aIsNotDefined = false;
                        }
                        else
                        {
                            //Checking Asymbol priority: if greater than, reassign Asymbol to A
                            var aSymbolPriority = this.stringGrammar.AttributeBySymbolAndName(aSymbol.Key, "priority").Value.Value;
                            if (aSymbolPriority > aPriority)
                            {
                                aPriority = aSymbolPriority;
                                a = aSymbol.Key;
                            }
                        }
                    }

                }
            } while (aIsNotDefined);


            this.errorStateS = s;
            this.onErrorRecoveryMode = true;
        }

        private void Shift(int value, Symbol symbol)
        {
            this.nodeStack.Push(value);
            var s = (Symbol) symbol.Clone();
            s.Attributes.Lexeme = symbol.Attributes.Lexeme;
            this.temporalStack.Push(s);
        }

        private void Reduce(int value)
        {
            var production = this.parser.SyntaxTable.ProductionById(value);
            var reversedStack = new Stack<Symbol>();

            for (var i = 0; i < production.Product.Count; i++)
            {
                this.nodeStack.Pop();
                reversedStack.Push(this.temporalStack.Pop());
            }
            var producer = (Symbol) production.Producer.Clone();
            this.productionSymbols.Add(producer);
            foreach (var symbol in reversedStack)
            {
                this.productionSymbols.Add(symbol);
            }

            if (this.compiledSemanticMethods != null && this.semanticMethods.ContainsKey(production) == true)
                this.compiledSemanticMethods.Call(this.semanticMethods[production]);
            //PrintProduction(production);
            this.productionSymbols.Clear();
            this.temporalStack.Push(producer);
            var goTo = this.parser.SyntaxTable[this.nodeStack.Peek(), production.Producer];
            this.nodeStack.Push(goTo.Item2);
        }

        private void PrintProduction(Production prod)
        {
            var builder = new StringBuilder();

            builder.Append(string.Format("[{0}] -> ", this.stringGrammar.GetSymbolName(prod.Producer)));
            foreach (var symbol in prod.Product)
            {
                builder.Append(string.Format("[{0}]", this.stringGrammar.GetSymbolName(symbol)));
            }
            Console.WriteLine(builder.ToString());
        }
    }
}
