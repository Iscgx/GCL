using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace GCL.Lex.Tests
{
    public class LexerTests
    {
        [Fact]
        public void A_lexer_maintains_behavior()
        {
            var grammarTokens = File.ReadAllText(@"TestData\GrammarTokens.txt");

            var lexer = new Lexer(grammarTokens);

            lexer.Parse(File.ReadAllText(@"TestData\SourceCode.txt"));

            lexer.TokenNames.Count().Should().Be(84);
        }
        [Fact]
        public void A_lexer_can_parse_an_return_an_enumerable_of_tokens()
        {
            var grammarTokens = File.ReadAllText(@"TestData\GrammarTokens.txt");

            var lexer = new Lexer(grammarTokens);

            lexer.TokenNames.Count().Should().Be(84);
            lexer.Parse(File.ReadAllText(@"TestData\SourceCode.txt")).Count().Should().Be(205);
        }
    }
}

