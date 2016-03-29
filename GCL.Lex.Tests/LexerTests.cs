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

            var tokenDispatchCount = 0;
            lexer.TokenCourier += _ => tokenDispatchCount++;
            lexer.Start(File.ReadAllText(@"TestData\SourceCode.txt"));

            lexer.TokenNames.Count().Should().Be(84);
            tokenDispatchCount.Should().Be(205);
        }
    }
}
