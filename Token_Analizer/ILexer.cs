using System;
using System.Collections.Generic;

namespace Token_Analizer
{
    public interface ILexer
    {
        List<string> TokenNames { get; }

        Action<Token> TokenCourier { get; set; }

        void Start(string sourceCode);
    }
}