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
        public static void Main(string[] args)
        {
            if (args.Count() == 0)
            {
                Console.WriteLine($"Usar: verificador [GR de entrada]");
                Environment.Exit(0);
            }

            #region FILE PRE PROCESSOR
            string filePath = args[0];

            using (var file = new StreamReader(filePath))
            {
                string content = "";
                content = file.ReadToEnd();
                content = Regex.Replace(content, @"\n|\r|\s+", "");

            }
            #endregion

        }

        public class Automata
        {
            public char[] Alphabet { get; set; }
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
            }
        }
    }
}
