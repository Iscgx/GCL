using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Token_Analizer;

namespace Syntax
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceCode = File.ReadAllText(@"SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"Tokens.txt");
            var grammarCode = File.ReadAllText(@"GrammarGCL.txt");
            var grammarTokens = File.ReadAllText(@"GrammarTokens.txt");
            var codeParser = new CodeParser(sourceTokens, grammarTokens, grammarCode);
            codeParser.Parse(sourceCode);
            Console.ReadLine();
        }

    }
}
