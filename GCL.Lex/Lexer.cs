using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
//using System.IO;

namespace GCL.Lex
{
    public class Lexer : ILexer
    {
        public List<string> TokenNames { get; private set; } //Token Names with corresponding ID (index)
        public Action<Token> TokenCourier { get; set; }

        private List<Automata> automatas;
        private int _string, lowlevel, cSharpCode, error, endOfFile, newLine; //Index of string token type, lowlevel type and EOF token type
        private string processedSourceCode;
        private readonly string tokenDefinitionCode;

        public Lexer(string tokenDefinitionCode/*, string sourceCode*/)
        {
            //if (!File.Exists(tokenDefinitionsFileName) || !File.Exists(sourceCodeFileName)) return;
            this.tokenDefinitionCode = tokenDefinitionCode;
            //_sourceCode = sourceCode;
            InitAllAutomata();
        }

        public void Start(string sourceCode)
        {
            PreProcessSourceCode(sourceCode);
            ProcessSourceCode(processedSourceCode);
        }

        private void InitAllAutomata()
        {
            var macros = new Dictionary<string, string>();
            TokenNames = new List<string>();
            var tokenRegexInt = new Dictionary<string, Tuple<string, int>>();
            var tokenIndex = 0;

            //var fileLines = File.ReadAllLines("Tokens.txt");
            var fileLines = tokenDefinitionCode.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var stringMacro = "";
            var stringDefinition = "";
            var definitions = true;

            foreach (var line in fileLines)
            {
                var stringTemporal = line;
                if (stringTemporal == "%%")
                {
                    definitions = false;
                    continue;
                }
                if (stringTemporal[0] == '/' && stringTemporal[1] == '/') continue;

                //Updating Dictionary of macros
                #region Reading Definitions

                if (definitions)
                {
                    for (var i = 0; i < stringTemporal.Length; i++)
                    {
                        if (stringTemporal[i] != ' ')
                            stringMacro += stringTemporal[i];
                        else
                        {
                            stringDefinition = stringTemporal.Substring(i + 1);
                            break;
                        }
                    }
                    if (stringDefinition[0] == '[')
                    {
                        var first = stringDefinition[1];
                        var last = stringDefinition[3];
                        var first2 = stringDefinition[4];
                        var last2 = first2 == ']' ? '\0' : stringDefinition[6];

                        stringDefinition = "(";
                        if (first > last)
                        {
                            var temp = first;
                            first = last;
                            last = temp;
                        }
                        for (; first < last; first++)
                        {
                            stringDefinition += first + "|";
                        }

                        if (first2 != ']')
                        {
                            stringDefinition += last + "|";
                            if (first > last)
                            {
                                var temp = first2;
                                first2 = last2;
                                last2 = temp;
                            }
                            for (; first2 < last2; first2++)
                            {
                                stringDefinition += first2 + "|";
                            }
                            stringDefinition += last2 + ")";
                        }
                        else
                            stringDefinition += last + ")";
                    }

                    macros[stringMacro] = stringDefinition;
                    stringMacro = "";
                    stringDefinition = "";
                }
                #endregion

                //Reading token definitions
                #region Reading Token Definitions (Regex)

                else
                {
                    var tokenName = "";
                    var regex = "";

                    for (var i = 0; i < stringTemporal.Length; i++)
                    {
                        if (stringTemporal[i] != ' ')
                            tokenName += stringTemporal[i];
                        else
                        {
                            regex = stringTemporal.Substring(i + 1);
                            break;
                        }
                    }

                    regex = regex == "" ? tokenName : regex;

                    var temporalRegex = "";
                    for (var i = 0; i < regex.Length; i++)
                    {
                        if ((regex[i] == '{' && i > 1 && regex[i - 1] != '\\') || (regex[i] == '{' && i == 0))
                        {
                            var tempMacroKey = "";
                            while (regex[++i] != '}')
                            {
                                tempMacroKey += regex[i];
                            }
                            temporalRegex += macros[tempMacroKey];
                        }
                        else
                        {
                            temporalRegex += regex[i];
                        }
                    }

                    regex = temporalRegex;

                    TokenNames.Add(tokenName);
                    tokenRegexInt[tokenName] = new Tuple<string, int>(regex, tokenIndex++);
                }

                #endregion
            }

            //Take List of all automata
            automatas =
                TokenNames.Select(
                    tokenName =>
                    AutomataCreator.GetAutomataFrom(tokenRegexInt[tokenName].Item1, tokenRegexInt[tokenName].Item2,
                                                    tokenName)).ToList();

            //Add integer values of String and low level
            //Don't add them to tokenNames dictionary so their corresponding automata is not created
            tokenRegexInt["string_value"] = new Tuple<string, int>("\".*\"", tokenIndex);
            _string = tokenIndex++;
            TokenNames.Add("string_value");
            tokenRegexInt["lowlevel"] = new Tuple<string, int>("@lowlevel{.*}@\"", tokenIndex);
            lowlevel = tokenIndex++;
            TokenNames.Add("lowlevel");
            tokenRegexInt["cSharpCode"] = new Tuple<string, int>("\\{.*\\}", tokenIndex);
            cSharpCode = tokenIndex++;
            TokenNames.Add("cSharpCode");
            tokenRegexInt["error"] = new Tuple<string, int>("error", tokenIndex);
            error = tokenIndex++;
            TokenNames.Add("error");
            tokenRegexInt["endOfFile"] = new Tuple<string, int>("endOfFile", tokenIndex);
            endOfFile = tokenIndex;
            TokenNames.Add("endOfFile");
        }

        private static bool IsWhitespace(char character)
        {
            return !(character != '\n' && character != '\t' && character != '\r' && character != '\b' && character != ' ');
        }

        private void CheckAllAutomata(ICollection<Token> tokens,int lineCount, string text)
        {
            if (tokens == null) throw new ArgumentNullException("tokens");
            var matchedToken = false;
            //Check all automata
            foreach (var automata in automatas.Where(automata => automata.GetifFinal()))
            {
                if (TokenCourier != null)
                {
                    var auto = automata.GetToken();
                    auto.Message = lineCount.ToString(CultureInfo.InvariantCulture);
                    TokenCourier(auto);
                }

                matchedToken = true;
                break;
            }

            if (!matchedToken)
            {
                if (text != "")
                {
                    if (TokenCourier != null)
                        TokenCourier(new Token(error, text,
                                               "Lexical error at line " + lineCount + ". Undefined token: \"" + text +
                                               "\""));
                }
            }

            foreach (var automata in automatas)
            {
                automata.Reset();
            }
        }

        private void ProcessSourceCode(string sourceCode)
        {
            var tokens = new List<Token>();

            if (sourceCode == null) return;

            #region Tokenize all SourceCode

            //Init all automata.
            foreach (var automaton in automatas)
            {
                automaton.Start();
            }

            var temporalLexeme = "";
            bool matchingLowLevel = false, matchingString = false, matchingCsharpCode = false;
            var lineCount = 1;
            var text = "";
            var lastI = 0;
            for (var i = 0; i <= sourceCode.Length; i++)
            {
                if(i == sourceCode.Length)
                {
                    if (!matchingString && !matchingLowLevel && !matchingCsharpCode)
                        CheckAllAutomata(tokens, lineCount, text);
                    else if (TokenCourier != null)
                            TokenCourier(new Token(error, temporalLexeme, "Lexical error at line " +lineCount + ". Undefined token: \"" + temporalLexeme + "\""));
                    continue;
                }

                if (IsWhitespace(sourceCode[i]))
                {
                    if (sourceCode[i] == '\n')
                    {
                        lineCount++;
                        if (matchingString)
                        {
                            matchingString = false;
                            i = lastI;
                        }
                }
                    if (!matchingLowLevel && !matchingString && !matchingCsharpCode)
                    {
                        CheckAllAutomata(tokens, lineCount, text);
                        text = "";
                        continue;
                    }
                }

                if (!matchingLowLevel && !matchingString && !matchingCsharpCode) //!false && !false
                {
                    //Not matching manual token definitions
                    switch(sourceCode[i])
                    {
                        case '@':
                            //Potential lowlevel
                            temporalLexeme = "";
                            var count = 0;
                            var max = i + 10;
                            for (var j = i;j < sourceCode.Length && j < max; j++, count++)
                            {
                                if (sourceCode[j] == ' ') max++;
                                else temporalLexeme += sourceCode[j];
                            }
                            if (temporalLexeme.Length >= 10 && temporalLexeme.Substring(0, 10) == "@lowlevel{")
                            {
                                i += count;
                                lastI = i;
                                temporalLexeme = "";
                                matchingLowLevel = true;
                            }
                            else
                            {
                                //Not lowlevel, continue moving in automatas
                                text += sourceCode[i];
                                foreach (var automata in automatas)
                                {
                                    automata.Move(sourceCode[i]);
                                }
                            }
                            break;
                        case '\"':
                            //Potential string
                            temporalLexeme = "\"";
                            lastI = i;
                            if(sourceCode[i - 1] != '\\')
                                matchingString = true;
                            else
                            {
                                //not string, continue moving in automatas
                                text += sourceCode[i];
                                foreach (var automata in automatas)
                                {
                                    automata.Move(sourceCode[i]);
                                }
                            }
                            break;
                        case '\\':
                            //Potential c sharp code
                            if(i+1 < sourceCode.Length && sourceCode[i+1] == '{')
                            {
                                matchingCsharpCode = true;
                                temporalLexeme = "";
                                i++;
                            }
                            else
                            {
                                //not c sharp code, continue moving in automatas
                                text += sourceCode[i];
                                foreach (var automata in automatas)
                                {
                                    automata.Move(sourceCode[i]);
                                } 
                            }
                            break;
                        default:
                            //Any character
                            text += sourceCode[i];
                            foreach (var automata in automatas)
                            {
                                automata.Move(sourceCode[i]);
                            }
                            break;
                    }

                }
                else if(matchingLowLevel) //true && --
                {
                    //Matching lowlevel
                    //Since we are matching lowlevel, we have to ignore everything until we get into }@
                    if(sourceCode[i] == '}')
                    {
                        var j = 0;
                        for (j = i + 1; j < sourceCode.Length && sourceCode[j] == ' '; j++){/*Empty Block*/}
                        if(j == sourceCode.Length - 1)
                        {
                            if (sourceCode[j] == '@')
                            {
                                matchingLowLevel = false;
                                if (TokenCourier != null)
                                    TokenCourier(new Token(lowlevel, temporalLexeme, lineCount.ToString(CultureInfo.InvariantCulture)));
                                i = j;
                            }
                            else
                            {
                                i = lastI;
                                matchingLowLevel = false;
                            }
                        }
                        else
                        {
                            if (j < sourceCode.Length && sourceCode[j] == '@')
                            {
                                matchingLowLevel = false;
                                if (TokenCourier != null)
                                    TokenCourier(new Token(lowlevel, temporalLexeme, lineCount.ToString(CultureInfo.InvariantCulture)));
                                i = j;
                            }
                            else
                            {
                                i = lastI;
                                matchingLowLevel = false;
                            }
                        }
                        
                    }
                    else
                    {
                        temporalLexeme += sourceCode[i];
                    }
                }
                else if(matchingCsharpCode)
                {
                    //Matching c sharp code
                    if(sourceCode[i] == '\\' && i+1 < sourceCode.Length && sourceCode[i+1]=='}')
                    {
                        i++;
                        matchingCsharpCode = false;
                        if (TokenCourier != null)
                            TokenCourier(new Token(cSharpCode, temporalLexeme, lineCount.ToString(CultureInfo.InvariantCulture), true));
                    }
                    else
                    {
                        temporalLexeme += sourceCode[i];
                    }
                }
                else
                {
                    //Matching string
                    temporalLexeme += sourceCode[i];
                    switch (sourceCode[i])
                    {
                        case '\\':
                            temporalLexeme += sourceCode[++i];
                            break;
                        case '\"':
                            matchingString = false;
                            if(TokenCourier != null)
                                TokenCourier(new Token(_string, temporalLexeme, lineCount.ToString(CultureInfo.InvariantCulture)));
                            break;
                    }
                }
            }
            #endregion

            if (TokenCourier != null)
                TokenCourier(new Token(endOfFile, "endOfFile", lineCount.ToString(CultureInfo.InvariantCulture)));
        }

        private void PreProcessSourceCode(string sourceCode)
        {
            //This method process the source code to transform it into a format which is easier to recognize for the lexical
            //analizer and all the automata

            //var sourceCode = _sourceCode;
                //Process in order to be able to recognize with the corresponding automata each operand defined
                var process = "";

                #region special cases
                for (var i = 0; i < sourceCode.Length; i++)
                {
                    var character = sourceCode[i];
                    switch (character)
                    {
                        case ';':
                        case '(':
                        case ')':
                        case '[':
                        case ']':
                        case '{':
                        case '}':
                        case '?':
                        case ',':
                            process += " " + character + " ";
                            break;

                        case ':':
                            if (i < sourceCode.Length - 1 && char.IsDigit(sourceCode[i + 1]))
                            {
                                process += character.ToString() + sourceCode[++i];
                            }
                            else
                            {
                                process += " " + character + " ";
                            }
                            break;

                        case '=':
                            if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '=' || sourceCode[i + 1] == '>'))
                                process += " " + character + "" + sourceCode[++i] + " ";
                            else
                                process += " " + character + " ";
                            break;

                        case '/':
                            if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '='))
                                process += " " + character + "" + sourceCode[++i] + " ";
                            else if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '/'))
                            {
                                while (i < sourceCode.Length && sourceCode[++i] != '\n')
                                {
                                    //Ignore everything until break line.
                                }
                                i--;
                            }
                            else if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '*'))
                            {
                                while (i < sourceCode.Length)
                                {
                                    if (sourceCode[i] == '\n')
                                    {
                                        process += sourceCode[i];
                                    }
                                    else if (sourceCode[i] == '*')
                                    {
                                        //Ignore everything until end of comment
                                        if (i <= sourceCode.Length - 1 && sourceCode[i + 1] == '/')
                                        {
                                            i++;
                                            break;
                                        }
                                    }
                                    i++;
                                }
                            }
                            else
                                process += " " + character + " ";
                            break;

                        case '!':
                        case '*':
                        case '%':
                        case '^':
                            if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '='))
                                process += " " + character + "" + sourceCode[++i] + " ";
                            else
                                process += " " + character + " ";
                            break;

                        case '|':
                        case '&':
                            if (i < sourceCode.Length - 1 &&
                                (sourceCode[i + 1] == '=' || sourceCode[i + 1] == character))
                                process += " " + character + "" + sourceCode[++i] + " ";
                            else
                                process += " " + character + " ";
                            break;

                        case '+':
                            if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '=' || sourceCode[i + 1] == '+'))
                            {
                                //++
                                // if (sourceCode[i + 1] == '+') //unary operator
                                //  process += "" + character + "" + sourceCode[++i] + "";
                                //else
                                process += " " + character + "" + sourceCode[++i] + " ";
                            }
                            else
                                process += " " + character + " ";
                            break;

                        case '-':
                            if (i < sourceCode.Length - 1 &&
                                (sourceCode[i + 1] == '=' || sourceCode[i + 1] == '>' || sourceCode[i + 1] == '-'))
                            {
                                //--
                                // if (sourceCode[i + 1] == '-') //unary operator
                                //   process += "" + character + "" + sourceCode[++i] + "";
                                // else
                                process += " " + character + "" + sourceCode[++i] + " ";
                            }
                            else
                                process += " " + character + " ";
                            break;

                        case '<':
                            //<=
                            if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '='))
                                process += " " + character + "" + sourceCode[++i] + " ";
                                //<<=
                            else if (i < sourceCode.Length - 2 && (sourceCode[i + 1] == '<' && sourceCode[i + 2] == '='))
                                process += " " + character + "" + sourceCode[++i] + "" + sourceCode[++i] + " ";
                                //<<
                            else if (i < sourceCode.Length - 2 &&
                                     (sourceCode[i + 1] == '<' && sourceCode[i + 2] != '='))
                                process += " " + character + "" + sourceCode[++i] + " ";
                                //<
                            else
                                process += " " + character + " ";
                            break;

                        case '>':
                            //>=
                            if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '='))
                                process += " " + character + "" + sourceCode[++i] + " ";
                                //><
                            else if (i < sourceCode.Length - 1 && (sourceCode[i + 1] == '<'))
                                process += " " + character + "" + sourceCode[++i] + " ";
                                //>|<
                            else if (i < sourceCode.Length - 2 && (sourceCode[i + 1] == '|' && sourceCode[i + 2] == '<'))
                            {
                                //>|<=
                                if (i + 3 < sourceCode.Length && sourceCode[i + 3] == '=')
                                    process += " " + character + "" + sourceCode[++i] + "" + sourceCode[++i] + "" +
                                               process[++i] + " ";
                                else
                                    process += " " + character + "" + sourceCode[++i] + "" + sourceCode[++i] + " ";
                            }
                                //>>=
                            else if (i < sourceCode.Length - 2 &&
                                     (sourceCode[i + 1] == '>' && sourceCode[i + 2] == '='))
                                process += " " + character + "" + sourceCode[++i] + "" + sourceCode[++i] + " ";
                                //>>
                            else if (i < sourceCode.Length - 2 &&
                                     (sourceCode[i + 1] == '>' && sourceCode[i + 2] != '='))
                                process += " " + character + "" + sourceCode[++i] + " ";
                                //>
                            else
                                process += " " + character + " ";
                            break;

                        case '\'':
                            process += '\'';
                            while (sourceCode[++i] != '\'')
                                process += sourceCode[i];
                            process += '\'';
                            break;

                        case '\\':
                            if (i < sourceCode.Length - 1 && sourceCode[i + 1] == '{')
                            {
                                //C sharp Code
                                var matched = false;
                                var j = i;
                                var temporal = "";
                                for(;j < sourceCode.Count();j++)
                                {
                                    if(sourceCode[j] == '\\' && j < sourceCode.Length - 1 && sourceCode[j + 1] == '}')
                                    {
                                        matched = true;
                                        temporal += "\\}";
                                        j += 1;
                                        break;
                                    }
                                    else
                                    {
                                        temporal += sourceCode[j];
                                    }
                                }

                                if(matched)
                                {
                                    i = j;
                                    process += temporal;
                                }
                            }   
                            else
                                process += character + "" + sourceCode[++i];
                            break;

                        case '.':
                            if (i - 1 > 0 && (sourceCode[i - 1] >= '0' && sourceCode[i - 1] <= '9'))
                                process += character;
                            else
                                process += " " + character + " ";
                            break;

                        case '@':
                            process += " " + character + " ";
                            break;

                        default:
                            process += character;
                            break;
                    }
                }

                #endregion

                processedSourceCode = process;
        }
    }
}
