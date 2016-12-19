using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GCL.Lex;
using GCL.Syntax.Data;
using Semantic;

namespace GCL.Syntax
{
    public delegate void OnLexicalError(string message);

    public delegate void OnSintacticalError(string message);

    public class CodeParser
    {
        private readonly StringGrammar stringGrammar;
        private readonly Parser parser;
        private readonly Stack<int> nodeStack = new Stack<int>();
        private readonly Stack<Symbol> temporalStack = new Stack<Symbol>();
        private readonly List<Symbol> productionSymbols = new List<Symbol>();

        private bool started = false;
        private DateTime parseStartTime;
        private bool accepted = true;
        private bool onErrorRecoveryMode;
        private int errorStateS;

        public OnLexicalError OnLexicalError;
        public OnSintacticalError OnSintacticalError;

        public CodeParser(StringGrammar stringGrammar,
            Parser parser)
        {
            this.stringGrammar = stringGrammar;
            this.parser = parser;
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
                this.OnLexicalError?.Invoke(token.Message);
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
                        ShiftAction(action, symbol);
                        break;
                    case ActionType.Accept:
                        AcceptAction();
                        break;
                    case ActionType.Reduce:
                        ReduceAction(token, actionCounts, action);
                        break;
                    case ActionType.Error:
                        ErrorAction(token);
                        break;
                    case ActionType.GoTo:
                        break;
                }
                //}  
            }
        }

        private void ErrorAction(Token token)
        {
            this.OnSintacticalError?.Invoke($@"Syntax error at line {token.Message}, token {token.Lexeme}.");
            Console.WriteLine(@"Syntax error at line {0}, near token {1}.", token.Message, token.Lexeme);
            Console.Write(@"Expecting token:");
            var first = true;

            var errorSymbols = this.stringGrammar.SymbolTable.Where(
                sym =>
                    sym.Value.Type == SymbolType.Terminal && this.parser.SyntaxTable.ContainsKey(this.nodeStack.Peek(), sym.Value)).Select(kvp => kvp.Key);

            foreach (var sym in errorSymbols)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    Console.Write("|");
                }
                Console.Write(@" ""{0}"" ", sym);
            }
            Console.WriteLine("\n");

            this.accepted = false;

            PanicModeErrorRecovery();
        }

        private void ReduceAction(Token token,
            Dictionary<ActionType, int> actionCounts,
            Tuple<ActionType, int> action)
        {
            var production = this.parser.SyntaxTable.ProductionById(action.Item2);
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

            this.productionSymbols.Clear();
            this.temporalStack.Push(producer);
            var goTo = this.parser.SyntaxTable[this.nodeStack.Peek(), production.Producer];
            this.nodeStack.Push(goTo.Item2);


            ParseToken(token, actionCounts);
        }

        private void AcceptAction()
        {
            Console.WriteLine(@"Parse: {0} ms", (DateTime.Now - this.parseStartTime).TotalMilliseconds);
        }

        private void ShiftAction(Tuple<ActionType, int> action,
            Symbol symbol)
        {
            this.nodeStack.Push(action.Item2);
            var s = (Symbol) symbol.Clone();
            s.Attributes.Lexeme = symbol.Attributes.Lexeme;
            this.temporalStack.Push(s);
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
    }
}
