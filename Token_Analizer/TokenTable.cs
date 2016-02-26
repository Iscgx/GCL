using System.Collections.Generic;

namespace Token_Analizer
{
    class TokenTable
    {
        public List<Token> Tokens { get; private set; } //Comes from source Code
        private readonly List<string> _tokenNames; //Name, int code

        public TokenTable(List<Token> tokens, List<string> tokenNames)
        {
            Tokens = tokens;
            _tokenNames = tokenNames;
        }

        public string this[int tokenType]
        {
            get { return tokenType < _tokenNames.Count && tokenType >= 0 ? _tokenNames[tokenType] : null; }
        }
    }
}
