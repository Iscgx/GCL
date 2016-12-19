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
            var errorsourceCode = File.ReadAllText(@"TestData\ErrorSourceCode.txt");

            var grammarCode = File.ReadAllText(@"TestData\GrammarGCL_Light.txt");
            var grammarTokens = File.ReadAllText(@"TestData\GrammarTokens.txt");

            var lexer = new Lexer(sourceTokens);

            ILexer readGrammarLexer = new Lexer(grammarTokens);
            ILexer codeLexer = lexer;
            DynamicCodeProvider dynamicCodeProvider = new DynamicCodeProvider();
            var semanticMethods = new Dictionary<Production, string>();

            var stringGrammar = new StringGrammar(codeLexer.TokenNames, dynamicCodeProvider, semanticMethods);

            foreach (var token in readGrammarLexer.Parse(grammarCode))
            {
                stringGrammar.AddSymbolDefinition(token);
            }

            stringGrammar.DefineTokens();

            var codeParser = new CodeParser(stringGrammar, new Parser(stringGrammar.Grammar));
            var actionCounts = codeParser.Parse(lexer.Parse(sourceCode));

            actionCounts[ActionType.Accept].Should().Be(1);
            actionCounts[ActionType.Reduce].Should().Be(320);
            actionCounts[ActionType.Shift].Should().Be(111);
            actionCounts.Should().NotContainKey(ActionType.Error);
            actionCounts.Should().NotContainKey(ActionType.GoTo);


            var errorActionCounts = codeParser.Parse(lexer.Parse(errorsourceCode));
            errorActionCounts[ActionType.Accept].Should().Be(1);
            errorActionCounts[ActionType.Reduce].Should().Be(296);
            errorActionCounts[ActionType.Shift].Should().Be(106);
            errorActionCounts[ActionType.Error].Should().Be(2);
            errorActionCounts.Should().NotContainKey(ActionType.GoTo);
        }

        [Fact]
        public static void StubMain()
        {
            var sourceCode = File.ReadAllText(@"TestData\SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"TestData\Tokens.txt");
            var grammarCode = File.ReadAllText(@"TestData\GrammarGCL_Light.txt");

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

        [Fact]
        public void CodeGeneration_PinningTest()
        {
            var sourceCode = File.ReadAllText(@"TestData/SourceCode.txt");
            var sourceTokens = File.ReadAllText(@"TestData/Tokens.txt");
            var grammarCode = File.ReadAllText(@"TestData/GrammarGCL_WithCG.txt");
            var grammarTokens = File.ReadAllText(@"TestData/GrammarTokens.txt");
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

            var gclCodeGenerator = new GclCodeGenerator();
            var semanticAnalysis = new SemanticAnalysis();

            dynamicCodeProvider.AddToScope(gclCodeGenerator, "codegen");
            dynamicCodeProvider.AddToScope(semanticAnalysis, "semantic");
            dynamicCodeProvider.AddToScope(semanticAnalysis.ThrowError, "ThrowError");

            var codeParserWithSupportForCodeGeneration = new CodeParserWithSupportForCodeGeneration(gclCodeGenerator,
                                                                                                    dynamicCodeProvider,
                                                                                                    semanticAnalysis,
                                                                                                    semanticMethods,
                                                                                                    stringGrammar,
                                                                                                    new Parser(
                                                                                                               stringGrammar
                                                                                                                   .Grammar));

            codeParserWithSupportForCodeGeneration.Parse(new Lexer(sourceTokens).Parse(sourceCode));

            var code = gclCodeGenerator.End();
            code.Should().Be(
@"#include <iostream>
#include <vector>
#include <cuda_runtime.h>

#pragma comment(lib, ""cudart"")

using namespace std;

double Pow(double _base, double _pow)
{
	if(_pow <= 0) return 1;
	int i = 0;
	for (i = 0; i < _pow; i++)
		_base *= _base;
	return _base;
}


__device__ double D_Pow(double _base, double _pow)
{
	if(_pow <= 0) return 1;
	int i = 0;
	for (i = 0; i < _pow; i++)
		_base *= _base;
	return _base;
}

__device__ int *d_a;
__device__ int *d_b;
__global__ void mapeishon(int *input, int *output)
{
	int x = blockIdx.x * blockDim.x + threadIdx.x;
	int y = blockIdx.y * blockDim.y + threadIdx.y;
	int z = blockIdx.z * blockDim.z + threadIdx.z;
	output[x] = (input[x] * input[x]);
}

int main()
{
	int *a = (int*)malloc(8192);
	int *b = (int*)malloc(8192);

		cudaMalloc (  & d_a , 4096 )  ; 
		cudaMalloc (  & d_b , 4096 )  ; 
		 
	int i = 0;
	for(;(i < 1024);i++)
	a[i] = i;
	cudaMemcpy(d_a, a, 8192, cudaMemcpyHostToDevice);
	mapeishon<<<2, 512>>>(d_a, d_b);
	cudaMemcpy(b, d_b, 4096, cudaMemcpyDeviceToHost);

		for ( int i  =  0 ; i  <  1024 ; i ++  ) 
			cout  <<  i  <<  "" )  ""  <<  b [ i ]   <<  endl ; 
		 
}

");
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
