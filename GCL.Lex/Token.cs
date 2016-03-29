namespace GCL.Lex
{
    public class Token
    {
        public int Type { get; private set; }
        public string Lexeme { get; private set; }
        public string Message { get; set; }
        public bool CsharpCode { get; private set; }

        public Token(int type, string lexeme, string message = "", bool csharpCode = false)
        {
            Type = type;
            Lexeme = lexeme;
            Message = message;
            CsharpCode = csharpCode;
        }

        public override string ToString()
        {
            return "" + Type + ", " + Lexeme;
        }
    }
}
