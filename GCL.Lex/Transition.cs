namespace GCL.Lex
{
    class Transition
    {
        //Field
        public char Symbol { get; set; }
        public State OutputState { get; set; }
        public bool IsCombo { get; set; }

        //Constructors
        public Transition(State outputState, char symbol)
        {
            OutputState = outputState;
            Symbol = symbol;
            IsCombo = false;
        }

        public Transition(State outputState, char symbol, bool isCombo)
        {
            OutputState = outputState;
            Symbol = symbol;
            IsCombo = isCombo;
        }

        //Methods
        public State Move(char symbol)
        {
            if (IsCombo)
                return OutputState;

            return Symbol == symbol ? OutputState : null;
        }

        public override string ToString()
        {
            return string.Format("{0} -> {1}", Symbol, OutputState);
        }
    }
}
