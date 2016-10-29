using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GrammarRecognition
{
    public class Program
    {
        public static List<string> MarkedStates = new List<string>();
        public static List<string> FinalLambdaStates = new List<string>();
        public static List<Tuple<string, char, string>> Transitions = new List<Tuple<string, char, string>>();

        public static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine($"Usar: verificador [GR de entrada]");
                Environment.Exit(0);
            }

            #region FILE PRE PROCESSOR
            string filePath = args[0];
            string grammar = "";

            using (var file = new StreamReader(filePath))
            {
                grammar = file.ReadToEnd();
                grammar = Regex.Replace(grammar, @"\n|\r|\s+", "");
                grammar = Regex.Replace(grammar, @"\s?}\s?,\s?", "};");
            }

            var automata = GrammarParser(grammar);
            var dfa = NfaToDfaConverter(automata);

            string word = "";
            while (word != "\u0004")
            {
                word = Console.ReadLine();

                var result = Recognize(dfa, word);

                if (result)
                {
                    Console.WriteLine("Sim");
                }
                else
                {
                    Console.WriteLine("Não");
                }
            }

            Environment.Exit(0);
            #endregion

        }

        private static bool Recognize(DeterministicAutomata dfa, string word)
        {
            var currentState = dfa.InitialNode;

            if (word == "#")
            {
                if (dfa.FinalNodes.Contains(currentState))
                {
                    return true;
                }

                return false;
            }

            foreach (var input in word)
            {
                var reached = dfa.Transitions.Where(x => x.Item1 == currentState && x.Item2 == input).Select(x => { return x.Item3; }).FirstOrDefault();

                if (reached == null)
                {
                    return false;
                }

                currentState = reached;
            }

            if (dfa.FinalNodes.Contains(currentState))
            {
                return true;
            }

            return false;
        }

        public class Automata
        {
            public List<char> Alphabet { get; set; }
            public List<string> InitialNode { get; set; }
            public List<string> States { get; set; }
            public List<Tuple<string, char, string>> Transitions { get; set; }
            public List<string> FinalNodes { get; set; }

            public Automata()
            {
                InitialNode = new List<string>();
                States = new List<string>();
                Transitions = new List<Tuple<string, char, string>>();
                FinalNodes = new List<string>();
                Alphabet = new List<char>();
            }
        }

        public class DeterministicAutomata
        {
            public List<char> Alphabet { get; set; }
            public string InitialNode { get; set; }
            public List<string> States { get; set; }
            public List<Tuple<string, char, string>> Transitions { get; set; }
            public List<string> FinalNodes { get; set; }

            public DeterministicAutomata()
            {
                States = new List<string>();
                Transitions = new List<Tuple<string, char, string>>();
                FinalNodes = new List<string>();
                Alphabet = new List<char>();
            }
        }

        public enum ENextSteps
        {
            Undefined,
            Begin,
            States,
            Alphabet,
            BuildTransitions,
            InitialStates,
            NormalizeTransitions,
            ReadTransitions
        }

        public static Automata GrammarParser(string program)
        {
            Stack<char> control = new Stack<char>();
            Stack<ENextSteps> steps = new Stack<ENextSteps>();

            steps.Push(ENextSteps.NormalizeTransitions);
            steps.Push(ENextSteps.InitialStates);
            steps.Push(ENextSteps.ReadTransitions);
            steps.Push(ENextSteps.Alphabet);
            steps.Push(ENextSteps.States);

            Automata automata = new Automata();
            ENextSteps currentStep = ENextSteps.Undefined;
            string processed = "";

            for (int i = 0; i < program.Length; i++)
            {
                var c = program[i];
                var cNext = '#';
                if (c == ';' || c == '(')
                {
                    if (steps.Count > 0)
                    {
                        currentStep = steps.Pop();
                        cNext = program[i + 1];
                    }
                    else
                    {
                        currentStep = ENextSteps.Undefined;
                    }
                }
                else
                {
                    if (c != ')')
                        cNext = program[i + 1];
                }

                switch (currentStep)
                {
                    case ENextSteps.Begin:
                        {
                            break;
                        }
                    case ENextSteps.States:
                        {
                            if (char.IsLetterOrDigit(c))
                            {
                                processed = processed + c;
                            }
                            if ((c == ',' || cNext == '}'))
                            {
                                automata.States.Add(processed);
                                processed = "";
                            }
                        }
                        break;
                    case ENextSteps.Alphabet:
                        {
                            if (char.IsLetterOrDigit(c))
                            {
                                processed = processed + c;
                            }
                            if (c == ',' || cNext == '}')
                            {
                                automata.Alphabet.Add(processed[0]);
                                processed = "";
                            }
                            break;
                        }
                    case ENextSteps.ReadTransitions:
                        {
                            if ((char.IsLetterOrDigit(c) || c == '#') && (cNext == '-' || cNext == ',' || cNext == '}'))
                            {
                                processed = processed + c;
                            }
                            if (c == '>' & char.IsLetterOrDigit(cNext))
                            {
                                processed = processed + cNext + '|';
                            }
                            if (c == '-' && cNext == '>')
                            {
                                processed = processed + '|';
                            }
                            if (c == '#')
                            {
                                processed = processed + "|#";
                            }
                            if (processed.Split('|').Length == 3 && processed.Split('|')[2] != "")
                            {
                                var itens = processed.Split('|');
                                if (automata.States.Contains(itens[0]) && automata.States.Contains(itens[2]) || automata.Alphabet.Contains(itens[2][0]) || itens[2][0] == '#')
                                {
                                    automata.Transitions.Add(new Tuple<string, char, string>(itens[0], itens[1].ToCharArray()[0], itens[2]));
                                    processed = "";
                                }
                            }
                            break;
                        }
                    case ENextSteps.InitialStates:
                        {
                            if (char.IsLetterOrDigit(c) && cNext != '-')
                            {
                                processed = processed + c;
                            }
                            if (automata.States.Contains(processed) && cNext == ')')
                            {
                                automata.InitialNode = new List<string>() { processed };
                                processed = "";

                                currentStep = ENextSteps.NormalizeTransitions;
                            }
                            break;
                        }
                    case ENextSteps.NormalizeTransitions:
                        {
                            var toNormalize = automata.Transitions.Where(x => automata.Alphabet.Contains(x.Item3[0]) || x.Item3[0] =='#');
                            if (toNormalize.Count() > 0)
                            {
                                var normalized = new List<Tuple<string, char, string>>();
                                automata.Transitions = automata.Transitions.Except(toNormalize).ToList();
                                FinalLambdaStates = toNormalize.Where(x => x.Item2 == '#' && x.Item3 == "#").Select(x => { return x.Item1.ToString(); } ).ToList();
                                var generate = true;
                                var state = GenerateState();

                                while (generate)
                                {
                                    if (!automata.States.Contains(state.ToString()))
                                    {
                                        automata.States.Add(state.ToString());
                                        normalized = toNormalize.Select(x => { return new Tuple<string, char, string>(x.Item1, x.Item2, state.ToString()); }).ToList();
                                        automata.FinalNodes.Add(state.ToString());
                                        generate = false;
                                    }
                                    else
                                    {
                                        state = GenerateState();
                                    }
                                }

                                automata.Transitions.AddRange(normalized);
                            }
                            break;
                        }
                    case ENextSteps.Undefined:
                        break;
                    default:
                        break;
                }
            }
            return automata;
        }

        public static char GenerateState()
        {
            Random _random = new Random();
            int num = _random.Next(0, 26);
            char let = (char)('a' + num);
            return char.ToUpper(let);
        }

        public static DeterministicAutomata NfaToDfaConverter(Automata nfa)
        {
            var transitions = new List<Tuple<List<string>, char, List<string>>>();
            var lambdaStates = new List<List<string>>();

            foreach (var state in nfa.States)
            {
                var achieved = LambdaDepth(state, nfa.Transitions);
                lambdaStates.Add(achieved);
            }

            var stateBegin = lambdaStates.Where(x => x.Intersect(nfa.InitialNode).Any()).FirstOrDefault();

            ReadState(string.Join("", stateBegin.ToArray()), nfa.Alphabet, nfa.Transitions);

            var dfa = new DeterministicAutomata();
            dfa.Alphabet = nfa.Alphabet;
            dfa.FinalNodes = Transitions.Where(x => x.Item1.Any(y => nfa.FinalNodes.Contains(y.ToString()))).Select(x => x.Item1).Distinct().Union(FinalLambdaStates).ToList();
            dfa.InitialNode = string.Join("", stateBegin.ToArray());
            dfa.States = Transitions.Where(x => !string.IsNullOrWhiteSpace(x.Item3)).Select(x => { return x.Item1; }).Union(Transitions.Where(x => !string.IsNullOrWhiteSpace(x.Item3)).Select(x => { return x.Item3; })).Distinct().ToList();
            dfa.Transitions = Transitions;

            return dfa;
        }

        public static List<string> LambdaDepth(string state, List<Tuple<string,char,string>> transitions)
        {
            var achieved = transitions
                .Where(x => x.Item1 == state && x.Item2 == '#')
                .Select(x => { return x.Item3; }).ToList();

            achieved.Add(state);

            return achieved;
        }

        public static string ReachStates(string states, char simbol, List<Tuple<string, char, string>> transitions)
        {
            var achieved = new List<string>();
            foreach (var state in states)
            {
                var reach = transitions.Where(x => x.Item1 == state.ToString() && x.Item2 == simbol);
                foreach (var item in reach)
                {
                    achieved.Add(item.Item3);
                }
            }

            return string.Join("", achieved.ToArray());
        }

        public static void ReadState(string state, List<char> simbols, List<Tuple<string, char, string>> transitions)
        {
            if (!MarkedStates.Contains(state) && state != null)
            {
                MarkedStates.Add(state);
                foreach (var simbol in simbols)
                {
                    var reached = ReachStates(state, simbol, transitions);
                    Transitions.Add(new Tuple<string, char, string>(state, simbol, reached));
                    if (!string.IsNullOrWhiteSpace(reached))
                    {
                        ReadState(reached, simbols, transitions);
                    }
                }
            }
        }
    }
}
