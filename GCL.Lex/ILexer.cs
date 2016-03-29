using System;
using System.Collections.Generic;

namespace GCL.Lex
{
    public interface ILexer
    {
        List<string> TokenNames { get; }

        Action<Token> TokenCourier { get; set; }

        void Start(string sourceCode);
    }
}