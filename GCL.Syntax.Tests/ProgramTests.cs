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
            var codeParser = new CodeParser(new Lexer(sourceTokens), 
                new GclCodeGenerator(),
                new DynamicCodeProvider(),
                new SemanticAnalysis(), readGrammarLexer.Parse(grammarCode));
            var code = codeParser.Parse(sourceCode);
            code.Length.Should().Be(415);
        }

        [Fact]
        public static void StubMain()
        {
            var sourceCode = File.ReadAllText(@"TestData\SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"TestData\Tokens.txt");
            var grammarCode = File.ReadAllText(@"TestData\GrammarGCL.txt");
            ILexer readGrammarLexer = new StubLexer();
            var codeParser = new CodeParser(new Lexer(sourceTokens),
                new GclCodeGenerator(),
                new DynamicCodeProvider(),
                new SemanticAnalysis(), readGrammarLexer.Parse(grammarCode));
            codeParser.Parse(sourceCode);
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
