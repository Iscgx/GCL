using System.Collections.Generic;
using System.Linq;

namespace GCL.Lex
{
    class State
    {
        //Field
        private readonly List<Transition> transitions;
        private bool isFinal;
        public int Id { get; set; }

        //Constructors
        public State(bool isFinal)
        {
            this.isFinal = isFinal;
            this.transitions = new List<Transition>();
        }

        //Methods
        public bool StateIsFinal()
        {
            return this.isFinal;
        }

        public void ChangeStateType(bool isFinal)
        {
            this.isFinal = isFinal;
        }

        public void AddTransition(State state, char symbol) {
            this.transitions.Add(new Transition(state, symbol));
        }

        public void AddTransition(State state, char symbol, bool isCombo)
        {
            this.transitions.Add(new Transition(state, symbol, isCombo));
        }

        public void AddTransition(Transition t)
        {
            AddTransition(t.OutputState, t.Symbol);
        }

        public List<State> Move(char symbol)
        {
            //Foreach transition return states that could move (List of states)
            return this.transitions.Select(t => t.Move(symbol)).Where(s => s != null).ToList();
        }

        public List<Transition> GetTransitions()
        {
            return this.transitions;
        }

        public void CopyTransitionsFrom(State externState)
        {
            if (Equals((externState)))
                return;
            foreach (var t in externState.GetTransitions())
            {
                AddTransition(t);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", Id, this.isFinal);
        }
    }
}
