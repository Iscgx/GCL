﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using GCL.Lex;
using GCL.Syntax;
using GCL.Syntax.Data;
using GCL.Syntax.Dynamic;
using Semantic;
using Xunit;
using CodeParser = GCL.Syntax.CodeParser;

namespace Syntax.Tests
{
    public class ProgramTests
    {
        [Fact]
        public static void Main()
        {
            var sourceCode = File.ReadAllText(@"TestData\SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"TestData\Tokens.txt");
            var grammarCode = File.ReadAllText(@"TestData\GrammarGCL.txt");
            var grammarTokens = File.ReadAllText(@"TestData\GrammarTokens.txt");

            var tokens = new Lexer(sourceTokens).Parse(sourceCode);

            ILexer readGrammarLexer = new Lexer(grammarTokens);
            ILexer codeLexer = new Lexer(sourceTokens);
            DynamicCodeProvider dynamicCodeProvider = new DynamicCodeProvider();
            var semanticMethods = new Dictionary<Production, string>();

            var stringGrammar = new StringGrammar(codeLexer.TokenNames, dynamicCodeProvider, semanticMethods);

            foreach (var token in readGrammarLexer.Parse(grammarCode))
            {
                stringGrammar.AddSymbolDefinition(token);
            }

            stringGrammar.DefineTokens();

            var codeParser = new CodeParser(stringGrammar, new Parser(stringGrammar.Grammar));
            var actionCounts = codeParser.Parse(tokens);

            actionCounts[ActionType.Accept].Should().Be(1);
            actionCounts[ActionType.Reduce].Should().Be(320);
            actionCounts[ActionType.Shift].Should().Be(111);
            actionCounts.Should().NotContainKey(ActionType.Error);
            actionCounts.Should().NotContainKey(ActionType.GoTo);

        }

        [Fact]
        public static void StubMain()
        {
            var sourceCode = File.ReadAllText(@"TestData\SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"TestData\Tokens.txt");
            var grammarCode = File.ReadAllText(@"TestData\GrammarGCL.txt");

            var tokens = new Lexer(sourceTokens).Parse(sourceCode);

            ILexer readGrammarLexer = new StubLexer();
            ILexer codeLexer = new Lexer(sourceTokens);
            DynamicCodeProvider dynamicCodeProvider = new DynamicCodeProvider();
            var semanticMethods = new Dictionary<Production, string>();
            var stringGrammar = new StringGrammar(codeLexer.TokenNames, dynamicCodeProvider, semanticMethods);

            foreach (var token in readGrammarLexer.Parse(grammarCode))
            {
                stringGrammar.AddSymbolDefinition(token);
            }

            stringGrammar.DefineTokens();

            var codeParser = new CodeParser(stringGrammar, new Parser(stringGrammar.Grammar));

            var actionCounts = codeParser.Parse(tokens);
            actionCounts.Should().NotContainKey(ActionType.Accept);
            actionCounts.Should().NotContainKey(ActionType.Reduce);
            actionCounts.Should().NotContainKey(ActionType.Shift);
            actionCounts.Should().NotContainKey(ActionType.Error);
            actionCounts.Should().NotContainKey(ActionType.GoTo);
        }
    }

    public class StubLexer : ILexer
    {
        public List<string> TokenNames => new List<string>();

        public Action<Token> TokenCourier { get; set; }

        public IEnumerable<Token> Parse(string sourceCode)
        {
            return Enumerable.Empty<Token>();
        }
    }
}
