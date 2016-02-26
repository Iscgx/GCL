using System;
using System.Reflection;

namespace gcl2.Dynamic
{
    public class CompiledClass
    {
        private readonly Assembly _compiledAssembly;
        private readonly object _instance;
        private readonly Type _classType;

        public CompiledClass(Assembly compiledAssembly, params object[] parameters)
        {
            _compiledAssembly = compiledAssembly;
            _classType = _compiledAssembly.GetType("DynamicCode");
            _instance = Activator.CreateInstance(_classType, parameters);
        }

        public void Call(string methodName, params object[] parameters)
        {
            try
            {
                _classType.GetMethod(methodName).Invoke(_instance, parameters);
            }
            catch (Exception e)
            {
                Console.WriteLine(@"CSharp Runtime error at method {0}: {1}", methodName, e.InnerException.Message);
            }
            
        }
    }
}
