using System;
using System.Collections.Generic;
using System.IO;
using GCL.Lex;
using GCL.Syntax.Data;
using GCL.Syntax.Dynamic;
using Semantic;

namespace GCL.Syntax
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceCode = File.ReadAllText(@"SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"Tokens.txt");
            var grammarCode = File.ReadAllText(@"GrammarGCL.txt");
            var grammarTokens = File.ReadAllText(@"GrammarTokens.txt");
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
            codeParser.Parse(new Lexer(sourceTokens).Parse(sourceCode));
            Console.ReadLine();
        }

    }
}
