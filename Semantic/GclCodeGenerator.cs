using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    public class GclCodeGenerator
    {
        private readonly StringBuilder builder;

        public GclCodeGenerator()
        {
            this.builder = new StringBuilder(10000);

            this.builder.Append("#include <iostream>\r\n" +
                           "#include <vector>\r\n" +
                           "#include <cuda_runtime.h>\r\n\r\n" +
                           "#pragma comment(lib, \"cudart\")\r\n\r\n" +
                           "using namespace std;\r\n");

            this.builder.Append("\r\ndouble Pow(double _base, double _pow)\r\n" +
                           "{\r\n\tif(_pow <= 0) return 1;\r\n" +
                           "\tint i = 0;\r\n" +
                           "\tfor (i = 0; i < _pow; i++)\r\n" +
                           "\t\t_base *= _base;\r\n" +
                           "\treturn _base;\r\n}\r\n\r\n");

            this.builder.Append("\r\n__device__ double D_Pow(double _base, double _pow)\r\n" +
                           "{\r\n\tif(_pow <= 0) return 1;\r\n" +
                           "\tint i = 0;\r\n" +
                           "\tfor (i = 0; i < _pow; i++)\r\n" +
                           "\t\t_base *= _base;\r\n" +
                           "\treturn _base;\r\n}\r\n\r\n");
        }

        public void AddCode(string partialCode)
        {
            this.builder.Append(partialCode);
        }

        public void AddTabs(int tabs)
        {
            for (int i = 0; i < tabs; i++)
            {
                this.builder.Append("\t");
            }
        }

        public string End()
        {
           return this.builder.ToString();
        }
    }
}
