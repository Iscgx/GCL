using System;
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

            ILexer readGrammarLexer = new Lexer(grammarTokens);
            ILexer codeLexer = new Lexer(sourceTokens);
            DynamicCodeProvider dynamicCodeProvider = new DynamicCodeProvider();
            var semanticMethods = new Dictionary<Production, string>();
            var codeParser = new CodeParser(codeLexer,
                new GclCodeGenerator(),
                dynamicCodeProvider,
                new SemanticAnalysis(),
                readGrammarLexer.Parse(grammarCode),
                semanticMethods,
                new StringGrammar(codeLexer.TokenNames, dynamicCodeProvider, semanticMethods));
            var code = codeParser.Parse(new Lexer(sourceTokens).Parse(sourceCode));
            code.Length.Should().Be(415);
        }

        [Fact]
        public static void StubMain()
        {
            var sourceCode = File.ReadAllText(@"TestData\SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"TestData\Tokens.txt");
            var grammarCode = File.ReadAllText(@"TestData\GrammarGCL.txt");
            ILexer readGrammarLexer = new StubLexer();
            ILexer codeLexer = new Lexer(sourceTokens);
            DynamicCodeProvider dynamicCodeProvider = new DynamicCodeProvider();
            var semanticMethods = new Dictionary<Production, string>();
            var codeParser = new CodeParser(codeLexer,
                new GclCodeGenerator(),
                dynamicCodeProvider,
                new SemanticAnalysis(),
                readGrammarLexer.Parse(grammarCode),
                semanticMethods,
                new StringGrammar(codeLexer.TokenNames, dynamicCodeProvider, semanticMethods));
            codeParser.Parse(new Lexer(sourceTokens).Parse(sourceCode));
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
