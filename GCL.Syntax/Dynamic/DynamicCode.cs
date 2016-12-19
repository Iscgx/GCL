using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GCL.Syntax.Dynamic
{
    public class DynamicCodeProvider
    {
        private readonly Dictionary<string, object> scopeVariables;
        private readonly List<string> scopeVariableList; 
        private readonly Dictionary<string, string> scopeMethods;
        private readonly Dictionary<string, string> methodsByCode; 
        private uint methodId = 0;

        public DynamicCodeProvider()
        {
            this.scopeVariables = new Dictionary<string, object>();
            this.scopeMethods = new Dictionary<string, string>();
            this.scopeVariableList = new List<string>();
            this.methodsByCode = new Dictionary<string, string>();
        }

        public string AddToScope(object o, string alias)
        {
            if (this.scopeVariables.ContainsKey(alias) == true)
                throw new ApplicationException("Scope variable alias already defined.");
            this.scopeVariableList.Add(alias);
            this.scopeVariables.Add(alias, o);
            return alias;
        }

        public object[] GetScopeVariables()
        {
            return this.scopeVariables.Values.ToArray();
        }

        public string AddMethod(string code)
        {
            code = code.Trim();
            if (this.methodsByCode.ContainsKey(code) == false)
            {
                var methodName = string.Format("f{0}", this.methodId);
                this.methodId += 1;
                this.scopeMethods.Add(methodName, code);
                this.methodsByCode.Add(code, methodName);
                return methodName;
            }
            return this.methodsByCode[code];       
        }

        public string GetCsCode()
        {
            var classcode = new StringBuilder("using System;\nusing System.Collections.Generic;\npublic class DynamicCode \n{\n");
            foreach (var scopeVariable in this.scopeVariables)
            {
                classcode.AppendLine(string.Format("\tprivate readonly {0} {1};", GetFriendlyTypeName(scopeVariable.Value.GetType()), scopeVariable.Key));
            }
            classcode.Append("\tpublic DynamicCode(");
            var i = 0;
            foreach (var scopeVariable in this.scopeVariableList)
            {
                classcode.Append(string.Format("{0} {1}", GetFriendlyTypeName(this.scopeVariables[scopeVariable].GetType()), scopeVariable));
                if (i < this.scopeVariables.Count - 1)
                {
                    classcode.Append(", ");
                }
                i += 1;
            }
            classcode.AppendLine(")\n\t{");
            foreach (var scopeVariable in this.scopeVariables)
            {
                classcode.AppendLine(string.Format("\t\tthis.{0} = {0};", scopeVariable.Key));
            }
            classcode.AppendLine("\t}");
            foreach (var scopeMethod in this.scopeMethods)
            {
                classcode.Append(string.Format("\tpublic void {0}() \n\t{{\n\t{1}\n\t}}\n", scopeMethod.Key, scopeMethod.Value));
            }
            classcode.AppendLine("}\n");
            return classcode.ToString();
        }

        public string GetFriendlyTypeName(Type type)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (type.IsGenericType == false)
            {
                return type.FullName;
            }

            var builder = new StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            builder.AppendFormat("{0}.{1}", type.Namespace, name.Substring(0, index));
            builder.Append('<');
            var first = true;
            foreach (var arg in type.GetGenericArguments())
            {
                if (first == false)
                    builder.Append(',');
                builder.Append(GetFriendlyTypeName(arg));
                first = false;
            }
            builder.Append('>');
            return builder.ToString();
        }

    }
}
