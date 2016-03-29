using System;
using System.Collections.Generic;
using System.Linq;

namespace GCL.Lex
{

    public enum AutomataTypes
    {
        Concatenation, Union, Closure, PositiveClosure
    }

    class Automata
    {
        #region Field
            //Field
            public List<State> States { get; private set; }
            public AutomataTypes AutomataType { get; set; }
            public string TokenDefinition { get; set; }
            public int TokenType { get; set; }
            public State InitialState { get; private set; }
            public List<State> ActualStates { get; private set; }
            public string TokenName { get; set; }
            public string TemporalTextSaver { get; set; }
        #endregion

        #region Constructors
        //Constructors
            public Automata(AutomataTypes type, string s)
            {
                DefaultValues();
                AutomataType = type;
                switch (AutomataType)
                {
                    case AutomataTypes.Concatenation:
                        ConstructConcatenation(s);
                        break;
                }
            }

            public Automata(AutomataTypes type, List<Automata> automaton)
            {
                DefaultValues();
                AutomataType = type;
                switch (AutomataType)
                {
                    case AutomataTypes.Concatenation:
                        ConstructConcatenation(automaton);
                        break;
                    case AutomataTypes.Union:
                        ConstructUnion(automaton);
                        break;
                }
            }

            public Automata(AutomataTypes type, Automata automata)
            {
                DefaultValues();
                AutomataType = type;
                switch (AutomataType)
                {
                    case AutomataTypes.Closure:
                        ConstructClosure(automata);
                        break;
                    case AutomataTypes.PositiveClosure:
                        ConstructPositiveClosure(automata);
                        break;
                }
            }

            private void DefaultValues()
        {
            States = new List<State>();
            TokenType = -1;
            TokenDefinition = "";
            ActualStates = new List<State>();
            InitialState = null;
            TemporalTextSaver = "";
        }
        #endregion

        #region Methods
            //Methods

            #region Construction Helping Methods

                private bool ClosureAlgorithm(int i, string s, State lastState, char symbol, bool isCombo = false)
                {
                    var j = i + 1;
                    if (j < s.Length)
                    {
                        if (s[j] == '*')
                        {
                            lastState.AddTransition(lastState, symbol, isCombo);
                            return true;
                        }
                        if (s[j] == '+')
                        {
                            var temporalState = new State(false);
                            States.Add(temporalState);
                            lastState.AddTransition(temporalState, symbol, isCombo);
                            temporalState.AddTransition(temporalState, symbol, isCombo);
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                //Method that creates a simple automata with concatenations of the elements of a string
                private void ConstructConcatenation(string s)
                {
                    //We need to init with a initial state by default
                    InitialState = new State(false);
                    States.Add(InitialState);

                    var lastState = InitialState;
                    var comilla = false;

                    //Cycles in string input
                    for (var i = 0; i < s.Length; i++)
                    {
                        bool createdClosure;
                        char symbol;
                        #region String Cycle Algorithm - Creating States
                            //If the character we are seeing is the escape ' operator, we enter here
                            if (s[i] == '\'')
                            {
                                comilla = true;
                                //Since is the escape character, we'll take the next character
                                //as the character we need to put in the transition
                                i++;
                                if (i == s.Length) break;
                                switch (s[i])
                                {
                                    case '\\':
                                        switch (s[i + 1])
                                        {
                                            case 'n':
                                                i++;
                                                symbol = '\n';
                                                break;
                                            case 't':
                                                i++;
                                                symbol = '\t';
                                                break;
                                            default:
                                                i++;
                                                symbol = s[i];
                                                break;
                                        }
                                        break;
                                    default:
                                        symbol = s[i];
                                        break;
                                }

                                createdClosure = ClosureAlgorithm(++i, s, lastState, symbol);
                                if (createdClosure)
                                {
                                    lastState = States.ElementAt(States.Count - 1);
                                    i++;
                                }
                                //We used one character and ignored two, so we moved two characters ahead
                            }
                            //If it wasn't the escape character, we come here
                            else
                            {
                                //It' a escape operator
                                if (s[i] == '\\')
                                {
                                    switch (s[i + 1])
                                    {
                                        case 'n':
                                            i++;
                                            symbol = '\n';
                                            break;
                                        case 't':
                                            i++;
                                            symbol = '\t';
                                            break;
                                        case '{':
                                            i++;
                                            symbol = '{';
                                            break;
                                        case '}':
                                            i++;
                                            symbol = '}';
                                            break;
                                        case '\'':
                                            i++;
                                            symbol = '\'';
                                            break;
                                        default:
                                            i++;
                                            symbol = s[i];
                                            break;
                                    }
                                }
                                //Since it wasn't the escape operator, we'll use the raw value
                                //of the actual character
                                else
                                {
                                    symbol = s[i];
                                }
                                var isCombo = symbol == '.' && !comilla;
                                createdClosure = ClosureAlgorithm(i, s, lastState, symbol, isCombo);
                                if (createdClosure)
                                {
                                    lastState = States.ElementAt(States.Count - 1);
                                    i++;
                                }
                            }

                            if (!createdClosure)
                            {
                                var temporalState = new State(false);
                                var isCombo = symbol == '.' && !comilla;
                                lastState.AddTransition(temporalState, symbol, isCombo);
                                States.Add(temporalState);
                                lastState = temporalState;
                            }
                        }
                    #endregion

                    //Last State created will be final
                    lastState.ChangeStateType(true);
                }

                //Method that creates an automata concatenating more than one automata in the order of appearence in the List
                private void ConstructConcatenation(List<Automata> automaton)
                {
                    if (automaton.Count <= 1) return;

                    //Get all final States from "last" automata
                    var finalStates = new List<State>();
                    var a = automaton.ElementAt(0);
                    InitialState = a.InitialState;
                    States.Clear();
                    foreach (var s in a.States)
                    {
                        if (s.StateIsFinal())
                        {
                            if(automaton.ElementAt((1)).AutomataType != AutomataTypes.Closure)
                                s.ChangeStateType(false);
                            finalStates.Add(s);
                        }

                        States.Add(s);
                    }

                    for (var i = 1; i < automaton.Count; i++)
                    {
                        //Copy transitions of the initial state from actual automata
                        //to all the final states of the last automata
                        a = automaton.ElementAt(i);
                        foreach (var s in finalStates)
                        {
                            s.CopyTransitionsFrom(a.InitialState);
                        }
                        //Clear the initial state from actual automata
                        //because it's not going to be used.
                        a.InitialState = null;


                        //Now all the final states are from actual automata unless it's a closure
                        finalStates.Clear();

                        foreach (State s in a.States)
                        {
                            if (s.StateIsFinal())
                            {
                                if (i != automaton.Count - 1)
                                {
                                    if (automaton.ElementAt((i + 1)).AutomataType != AutomataTypes.Closure)
                                        s.ChangeStateType(false);
                                    finalStates.Add(s);
                                }
                            }
                            States.Add(s);
                        }
                    }
                }

                private void ConstructUnion(List<Automata> automaton)
                {
                    if (automaton.Count <= 1) return;
                    //create new Initial_state, which is who will connect all automata in automaton
                    InitialState = new State(false);
                    //If any of the automata in automaton is CLOSURE, initial state is too
                    if (automaton.Any(a => a.AutomataType == AutomataTypes.Closure))
                    {
                        InitialState.ChangeStateType(true);
                    }
                    States.Add(InitialState);
                    //Is just needed to copy all transitions of all automaton initial states
                    //to this new initial_state
                    foreach (Automata a in automaton)
                    {
                        InitialState.CopyTransitionsFrom(a.InitialState);
                        foreach (State s in a.States)
                            States.Add(s);
                    }
                }

                private void ConstructClosure(Automata automata)
                {
                    //First we have to copy exactly all the automata
                    InitialState = automata.InitialState;
                    var finalStates = new List<State>();
                    foreach (State s in automata.States)
                    {
                        States.Add(s);
                        //We retrieve all final states
                        if (s.StateIsFinal())
                            finalStates.Add(s);
                    }
            
                    //Since is closure, we just have to copy all transitions from initial state
                    //to all my final states and make initial state final
                    foreach (State s in finalStates)
                    {
                        s.CopyTransitionsFrom(InitialState);
                    }

                    InitialState.ChangeStateType(true);
                }

                private void ConstructPositiveClosure(Automata automata)
            {
                //First we have to copy exactly all the automata
                InitialState = automata.InitialState;
                var finalStates = new List<State>();
                foreach (var s in automata.States)
                {
                    States.Add(s);
                    //We retrieve all final states
                    if (s.StateIsFinal())
                        finalStates.Add(s);
                }

                //Since is positive closure, we just have to copy all transitions from initial state
                //to all my final states
                foreach (State s in finalStates)
                {
                    s.CopyTransitionsFrom(InitialState);
                }
            }

            #endregion

            #region Support Methods

                private static void PrintState(State moving)
                {
                    Console.Write("State: " + moving.StateIsFinal() + " ");
                    foreach (var t in moving.GetTransitions())
                    {
                        Console.Write(t.Symbol);
                    }
                }

                public void Print()
                {
                    Console.WriteLine("Automata with token_definition: " + TokenDefinition);
                    Console.Write("\n\t\tAutomata definition:\n\n");
                    foreach (var s in States)
                    {
                        PrintState(s);
                        Console.WriteLine();
                    }
                    Console.WriteLine();
                }

                public void SetIdforStates()
                {
                    var i = 0;
                    foreach (var s in States)
                    {
                        s.Id = i++;
                    }
                }

                public override string ToString()
            {
                return TokenName + ", " + TokenDefinition;
            }

            #endregion

            #region Moving Methods

                public List<State> GetActualStates()
                {
                    return ActualStates;
                }

                public bool GetifFinal()
                {
                    return ActualStates.Any(s => s.StateIsFinal());
                }

                public Token GetToken()
                {
                    return new Token(TokenType, TemporalTextSaver);
                }

                public bool Move(char symbol)
                {

                    TemporalTextSaver += symbol;

                    if(ActualStates.Count == 0) return false;

                    var temporalStates = new List<State>();
                    foreach (var temporal in ActualStates.Select(s => s.Move(symbol)))
                    {
                        temporalStates.AddRange(temporal);
                    }

                    ActualStates = temporalStates;

                    if(ActualStates.Count == 0) 
                    {
                        TemporalTextSaver = "";
                        return false;
                    }
                    return true;
                }

                public void Start()
                {
                    Reset();
                }

                public void Reset()
                {
                    ActualStates.Clear();
                    TemporalTextSaver = "";
                    ActualStates.Add(InitialState);
                }

                public bool Accepts(string input)
            {
                Start();
                foreach (var t in input)
                {
                    Move(t);
                    if (!ActualStates.Any())
                        return false;
                }

                return ActualStates.Count(p => p.StateIsFinal()) > 0;
            }

            #endregion

        #endregion
    }
}
