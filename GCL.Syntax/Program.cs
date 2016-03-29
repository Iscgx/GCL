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

            var stringGrammar = new StringGrammar(codeLexer.TokenNames, dynamicCodeProvider, semanticMethods);

            foreach (var token in readGrammarLexer.Parse(grammarCode))
            {
                stringGrammar.AddSymbolDefinition(token);
            }

            stringGrammar.DefineTokens();

            var codeParser = new CodeParser(new GclCodeGenerator(),
                dynamicCodeProvider,
                new SemanticAnalysis(),
                semanticMethods,
                stringGrammar);
            codeParser.Parse(new Lexer(sourceTokens).Parse(sourceCode));
            Console.ReadLine();
        }

    }
}
