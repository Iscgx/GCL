using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    class DeviceFunction
    {
        public string Name { get; set; }
        public List<string> Parameters { get; set; }
        public string Pattern { get; set; }
        public int Blocks { get; set; }
        public int Threads { get; set; }

        public DeviceFunction(string name, List<string> parameters, string pattern)
        {
            Name = name;
            Parameters = parameters;
            Pattern = pattern;
            Blocks = 1;
            Threads = 1;

        }
    }
}
