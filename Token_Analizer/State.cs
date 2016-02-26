using System.Collections.Generic;
using System.Linq;

namespace Token_Analizer
{
    class State
    {
        //Field
        private readonly List<Transition> _transitions;
        private bool _isFinal;
        public int Id { get; set; }

        //Constructors
        public State(bool isFinal)
        {
            _isFinal = isFinal;
            _transitions = new List<Transition>();
        }

        //Methods
        public bool StateIsFinal()
        {
            return _isFinal;
        }

        public void ChangeStateType(bool isFinal)
        {
            _isFinal = isFinal;
        }

        public void AddTransition(State state, char symbol) {
            _transitions.Add(new Transition(state, symbol));
        }

        public void AddTransition(State state, char symbol, bool isCombo)
        {
            _transitions.Add(new Transition(state, symbol, isCombo));
        }

        public void AddTransition(Transition t)
        {
            AddTransition(t.OutputState, t.Symbol);
        }

        public List<State> Move(char symbol)
        {
            //Foreach transition return states that could move (List of states)
            return _transitions.Select(t => t.Move(symbol)).Where(s => s != null).ToList();
        }

        public List<Transition> GetTransitions()
        {
            return _transitions;
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
            return string.Format("{0}, {1}", Id, _isFinal);
        }
    }
}
