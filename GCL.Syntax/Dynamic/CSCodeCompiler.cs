using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.CSharp;

namespace GCL.Syntax.Dynamic
{
    static class CsCodeCompiler
    {
        public static CompiledClass Compile(DynamicCodeProvider dynamicCodeProvider, params string[] assemblies)
        {
            var options = new Dictionary<string, string> {{"CompilerVersion", "v4.0"}};
            var compiler = new CSharpCodeProvider(options);
            var parameters = new CompilerParameters
                {
                    WarningLevel = 4,
                    GenerateExecutable = false,
                    GenerateInMemory = true
                };
            foreach (var assembly in assemblies)
                parameters.ReferencedAssemblies.Add(assembly);
            
            var compilerResults = compiler.CompileAssemblyFromSource(parameters, dynamicCodeProvider.GetCsCode());
            foreach (var error in compilerResults.Errors)
            {
                Console.WriteLine(error);
            }
            var compiledAssembly = compilerResults.CompiledAssembly;
            return new CompiledClass(compiledAssembly, dynamicCodeProvider.GetScopeVariables());
        }
    }
}
