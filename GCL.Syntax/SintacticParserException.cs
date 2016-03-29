using System;

namespace GCL.Syntax
{
    [Serializable]
    public class SintacticParserException : ApplicationException
    {
        public SintacticParserException(string message) : base(message)
        {

        }
    }
}
