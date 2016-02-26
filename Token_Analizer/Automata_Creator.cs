using System.Collections.Generic;
using System.Linq;

namespace Token_Analizer
{
    static class AutomataCreator
    {
        //Method that creates a new automata from a regular expression given.
        public static Automata GetAutomataFrom(string regex, int tokenType, string tokenName)
        {
            var initialParse = new List<string>(); //Listing of the initial parse for doing simplier our tasks
            var automatas = new List<Automata>(); //Listing of automatas that are being created

            //This loop gets all the simpliest automatas ignoring regex operators
            #region Initial parse
            var buffer = "";
            for (var i = 0; i < regex.Length; i++)
            {
                var a = regex[i];
                switch (a)
                {
                    case '(':
                    case ')':
                    case '|':
                        if(i > 1 && regex[i - 1] == '\\')
                        {
                            buffer += a;
                            continue;
                        }
                        if (buffer != "")
                        {
                            automatas.Add(new Automata((int) AutomataTypes.Concatenation, buffer));

                            buffer = "";
                            initialParse.Add("" + (automatas.Count - 1));
                        }
                        initialParse.Add("" + a);
                        break;

                    case '+':
                    case '*':
                        if (i - 1 >= 0)
                        {
                            if (regex[i - 1] == ')')
                            {
                                if (buffer != "")
                                {
                                    automatas.Add(new Automata(AutomataTypes.Concatenation, buffer));

                                    buffer = "";
                                    initialParse.Add("" + (automatas.Count - 1));
                                }
                                initialParse.Add("" + a);
                            }
                            else
                            {
                                buffer += a;
                            }
                        }
                        break;

                    case '\\':
                            switch (regex[i + 1])
                            {
                                case 'n':
                                    i++;
                                    buffer += '\n';
                                    break;
                                case 't':
                                    i++;
                                    buffer += '\t';
                                    break;
                                case '\'':
                                case '\\':
                                    buffer += "\\" + regex[++i];
                                    break;
                                default:
                                    buffer += regex[++i];
                                    break;
                            }
                    break;

                    case '\'':
                        buffer += "\'" + regex[++i] + "" + regex[++i];
                        break;

                    default:
                        //None of the above
                        buffer += a;
                        break;
                }
            }

            if (buffer != "")
            {
                automatas.Add(new Automata(AutomataTypes.Concatenation, buffer));

                initialParse.Add("" + (automatas.Count - 1));
            }
            #endregion
            
            //After having out initial parse, we'll use a pseudo LR parser that will
            //create our automata by reading our initial parse position by position
            //This is basically used to know aggrupation.
            #region intermediate parse
            var parser = new Stack<string>();
            parser.Push("$");
            for (int i = 0; i < initialParse.Count ; i++)
            {
                string s = initialParse.ElementAt(i);
                //If opening parenthesis, just push it
                if (s == "(")
                {
                    parser.Push(s);
                }
                else if (s == ")")
                {
                    //If closing parenthesis, pop until you find an opening parenthesis. 
                    //Create new automata, add it to automatas and push it to the parser
                    string temporal = parser.Pop();
                    string last = "";
                    var temporalStack = new Stack<Automata>();
                    bool union = false;
                    while (temporal != "(")
                    {
                        if (temporal != "|")
                        {
                            //If not a union operator, then it's an automata index
                            temporalStack.Push(automatas.ElementAt(int.Parse(temporal)));
                        }
                        else 
                        {
                            //It was a Union operator
                            union = true;
                        }
                        last = temporal;
                        temporal = parser.Pop();
                    }

                    if (temporalStack.Count > 1)
                    {
                        automatas.Add(union
                                          ? new Automata(AutomataTypes.Union, temporalStack.ToList())
                                          : new Automata(AutomataTypes.Concatenation, temporalStack.ToList()));

                        //Push the last automata index
                        parser.Push("" + (automatas.Count - 1));
                    }
                    else
                    {
                        parser.Push(last);
                    }
                }
                else if (s == "*")
                {
                    var temporal = parser.Pop(); //pop automata index

                    //Add new automata, which is the same automata but with closure
                    automatas.Add(new Automata(AutomataTypes.Closure, automatas.ElementAt(int.Parse(temporal))));

                    //Push the last automata index
                    parser.Push("" + (automatas.Count - 1));
                }
                else if (s == "+")
                {
                    var temporal = parser.Pop(); //pop automata index

                    //Add new automata, which is the same automata but with closure
                    automatas.Add(new Automata(AutomataTypes.PositiveClosure, automatas.ElementAt(int.Parse(temporal))));

                    //Push the last automata index
                    parser.Push("" + (automatas.Count - 1));
                }
                else
                {
                    //Is an automata index or a Union operator
                    parser.Push(s);
                }
            }
            #endregion

            //In here, we don't have any more parenthesis nor closure operators
            //so we just have to pop everything and create a solo automata
            //that will be a concatenation or a union
            #region final parse
            string lastS = parser.Pop();
            string lastIndex = "";
            var temporalAutomataStack = new Stack<Automata>();
            bool concatenation = true;
            while (lastS != "$")
            {
                if (lastS != "|")
                {
                    //If not a union operator, then it's an automata index
                    temporalAutomataStack.Push(automatas.ElementAt(int.Parse(lastS)));
                }
                else
                {
                    //It was a Union operator
                    concatenation = false;
                }
                lastIndex = lastS;
                lastS = parser.Pop();
            }

            Automata retAutomata;

            if (temporalAutomataStack.Count > 1)
            {
                automatas.Add(concatenation
                                  ? new Automata(AutomataTypes.Concatenation, temporalAutomataStack.ToList())
                                  : new Automata(AutomataTypes.Union, temporalAutomataStack.ToList()));

                //return the last automata created
                retAutomata = automatas.ElementAt(automatas.Count - 1);
            }
            else
            {
                //return the automata with index last_s
                retAutomata = automatas.ElementAt(int.Parse(lastIndex));
            }

            retAutomata.TokenDefinition = regex;
            retAutomata.TokenType = tokenType;
            retAutomata.TokenName = tokenName;
            retAutomata.SetIdforStates();

            #endregion

            return retAutomata;
        }
    }
}
