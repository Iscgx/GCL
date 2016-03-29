using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using Microsoft.CSharp;

namespace Syntax.Dynamic
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
            
            var r = compiler.CompileAssemblyFromSource(parameters, dynamicCodeProvider.GetCsCode());
            foreach (var error in r.Errors)
            {
                Console.WriteLine(error);
            }
            var compiledAssembly = r.CompiledAssembly;
            return new CompiledClass(compiledAssembly, dynamicCodeProvider.GetScopeVariables());
        }
    }
}
