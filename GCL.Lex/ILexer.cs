using System;
using System.Collections.Generic;

namespace GCL.Lex
{
    public interface ILexer
    {
        List<string> TokenNames { get; }

        IEnumerable<Token> Parse(string sourceCode);
    }
}