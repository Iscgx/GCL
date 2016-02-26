using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Semantic
{
    [Flags]
    public enum TypeParameters
    {
        None = 0,
        IsArithmetic = 1,
        IsBitwise = 2,
        IsInteger = 3,
        IsUnsigned = 4,
        IsArray = 8,
        IsStructure = 16
    }
}
