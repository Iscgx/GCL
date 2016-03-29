using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Syntax
{
    [Serializable]
    public class SintacticParserException : ApplicationException
    {
        public SintacticParserException(string message) : base(message)
        {

        }
    }
}
