using System;
using System.Collections.Generic;
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
            nodeStack.Clear();
            nodeStack.Push(0);
            temporalStack.Clear();

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
            if (started == false)
            {
                parseStartTime = DateTime.Now;
                started = true;
            }
            if (stringGrammar.TokenDictionary.ContainsKey(token.Type) == false)
            {
                OnLexicalError?.Invoke(token.Message);
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
            OnSintacticalError?.Invoke($@"Syntax error at line {token.Message}, token {token.Lexeme}.");
            Console.WriteLine(@"Syntax error at line {0}, near token {1}.", token.Message, token.Lexeme);
            Console.Write(@"Expecting token:");
            var first = true;
            foreach (var sym in stringGrammar.SymbolTable)
            {
                if (sym.Value.Type == SymbolType.Terminal && parser.SyntaxTable.ContainsKey(nodeStack.Peek(), sym.Value))
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

            accepted = false;

            PanicModeErrorRecovery();
        }

        private void ReduceAction(Token token,
            Dictionary<ActionType, int> actionCounts,
            Tuple<ActionType, int> action)
        {
            Reduce(action.Item2);
            ParseToken(token, actionCounts);
        }

        private void AcceptAction()
        {
            Console.WriteLine(@"Parse: {0} ms", (DateTime.Now - parseStartTime).TotalMilliseconds);
        }

        private void ShiftAction(Tuple<ActionType, int> action,
            Symbol symbol)
        {
            Shift(action.Item2, symbol);
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

            productionSymbols.Clear();
            temporalStack.Push(producer);
            var goTo = parser.SyntaxTable[nodeStack.Peek(), production.Producer];
            nodeStack.Push(goTo.Item2);
        }
    }
}
