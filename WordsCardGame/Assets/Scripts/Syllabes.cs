using System.Collections.Generic;

namespace Palabrario
{
    public static class Syllables
    {
        static readonly HashSet<char> StrongVowels = new HashSet<char>("aeoáéó");
        static readonly HashSet<char> WeakAccentedVowels  = new HashSet<char>("íú");     
        static readonly HashSet<char> WeakVowels    = new HashSet<char>("iu");
        static readonly HashSet<char> AccentedVowels = new HashSet<char>("áéíóú");
        static readonly HashSet<char> AllVowels;

        static Syllables()
        {
            AllVowels = new HashSet<char>();
            AllVowels.UnionWith(StrongVowels);
            AllVowels.UnionWith(WeakAccentedVowels);
            AllVowels.UnionWith(WeakVowels);
        }

        static string Preprocess(string w)
        {
            w = w.ToLowerInvariant();
            w = w.Replace("qu", "k");
            w = w.Replace("gue", "ge").Replace("gui", "gi");
            w = w.Replace("güe", "gue").Replace("güi", "gui").Replace("ü", "u");
            
            if (w == "y") return "i";
            if (w.EndsWith("y")) return w.Substring(0, w.Length - 1) + "i";
            return w;
        }

        static bool FormDiphthong(char v1, char v2)
        {
            if (StrongVowels.Contains(v1) && StrongVowels.Contains(v2)) return false;  
            if (WeakAccentedVowels.Contains(v1) || WeakAccentedVowels.Contains(v2)) return false;  
            return true;
        }

        class Nucleus { public char LastChar; public bool IsAccented; }

        public static (int syllableCount, string stressType) Analyze(string word)
        {
            string w = Preprocess(word);
            var nuclei = new List<Nucleus>();
            Nucleus current = null;

            foreach (char ch in w)
            {
                if (AllVowels.Contains(ch))
                {
                    bool isAc = AccentedVowels.Contains(ch);
                    if (current != null && FormDiphthong(current.LastChar, ch))
                    {
                        current.LastChar = ch;
                        if (isAc) current.IsAccented = true;
                    }
                    else
                    {
                        current = new Nucleus { LastChar = ch, IsAccented = isAc };
                        nuclei.Add(current);
                    }
                }
                else
                {
                    current = null; 
                }
            }

            int count = nuclei.Count;
            if (count == 0) return (0, null);

            int tonicIdx = -1;
            for (int i = 0; i < count; i++)
                if (nuclei[i].IsAccented) { tonicIdx = i; break; }

            if (tonicIdx == -1)
            {
                if (count == 1) tonicIdx = 0;
                else
                {
                    char lastChar = word.ToLowerInvariant()[word.Length - 1];
                    bool vowelOrNS = "aeiouáéíóú".IndexOf(lastChar) >= 0 || lastChar == 'n' || lastChar == 's';
                    tonicIdx = vowelOrNS ? count - 2 : count - 1;  
                }
            }

            int fromEnd = (count - 1) - tonicIdx;
            string stress = fromEnd == 0 ? "aguda"
                          : fromEnd == 1 ? "llana"
                          : fromEnd == 2 ? "esdrujula"
                          : "sobreesdrujula";
            return (count, stress);
        }

        public static string RunTests()
        {
            var cases = new (string w, int sil, string ac)[]
            {
                ("casa",2,"llana"), ("crepúsculo",4,"esdrujula"), ("inefable",4,"llana"),
                ("raíz",2,"aguda"), ("día",2,"llana"), ("país",2,"aguda"),
                ("ciudad",2,"aguda"), ("murciélago",4,"esdrujula"), ("petricor",3,"aguda"),
                ("transporte",3,"llana"), ("perro",2,"llana"), ("examen",3,"llana"),
                ("obstáculo",4,"esdrujula"), ("química",3,"esdrujula"), ("guerra",2,"llana"),
                ("pingüino",3,"llana"), ("vergüenza",3,"llana"), ("corazón",3,"aguda"),
                ("árbol",2,"llana"), ("estoy",2,"aguda"), ("luz",1,"aguda"), ("continúo",4,"llana"),
            };
            int ok = 0;
            foreach (var c in cases)
            {
                var r = Analyze(c.w);
                if (r.syllableCount == c.sil && r.stressType == c.ac) ok++;
            }
            return $"{ok}/{cases.Length} correct";
        }
    }
}