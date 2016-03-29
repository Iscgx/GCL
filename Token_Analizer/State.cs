using System.Collections.Generic;
using System.Linq;

namespace Token_Analizer
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
            transitions = new List<Transition>();
        }

        //Methods
        public bool StateIsFinal()
        {
            return isFinal;
        }

        public void ChangeStateType(bool isFinal)
        {
            this.isFinal = isFinal;
        }

        public void AddTransition(State state, char symbol) {
            transitions.Add(new Transition(state, symbol));
        }

        public void AddTransition(State state, char symbol, bool isCombo)
        {
            transitions.Add(new Transition(state, symbol, isCombo));
        }

        public void AddTransition(Transition t)
        {
            AddTransition(t.OutputState, t.Symbol);
        }

        public List<State> Move(char symbol)
        {
            //Foreach transition return states that could move (List of states)
            return transitions.Select(t => t.Move(symbol)).Where(s => s != null).ToList();
        }

        public List<Transition> GetTransitions()
        {
            return transitions;
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
            return string.Format("{0}, {1}", Id, isFinal);
        }
    }
}
