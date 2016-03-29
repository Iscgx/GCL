using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GCL.Syntax;
using Token_Analizer;
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
            var codeParser = new CodeParser(sourceTokens, grammarCode, new Lexer(grammarTokens));
            codeParser.Parse(sourceCode);
        }
    }
}
