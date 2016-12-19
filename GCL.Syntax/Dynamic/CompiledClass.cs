using System;
using System.Reflection;

namespace GCL.Syntax.Dynamic
{
    public class CompiledClass
    {
        private readonly Assembly compiledAssembly;
        private readonly object instance;
        private readonly Type classType;

        public CompiledClass(Assembly compiledAssembly, params object[] parameters)
        {
            this.compiledAssembly = compiledAssembly;
            this.classType = this.compiledAssembly.GetType("DynamicCode");
            this.instance = Activator.CreateInstance(this.classType, parameters);
        }

        public void Call(string methodName, params object[] parameters)
        {
            try
            {
                this.classType.GetMethod(methodName).Invoke(this.instance, parameters);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"CSharp Runtime error at method {0}: {1}", methodName, e.InnerException.Message);
            }
            
        }
    }
}
