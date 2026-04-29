// Preprocesses source input for the interpreter.
// - Removes `%%` comments and blank lines
// - Produces (lineNumber, cleanedLine) pairs
// - Holds the set of LEXOR reserved words

namespace LexorInterpreter.ProgramCodes
{
    public static class Lexer
    {
        // Every reserved word in the LEXOR language
        public static readonly HashSet<string> ReservedWords = new()
        {
            "SCRIPT", "AREA", "START", "END", "DECLARE",
            "PRINT", "SCAN", "INT", "CHAR", "BOOL", "FLOAT",
            "TRUE", "FALSE", "AND", "OR", "NOT",
            "IF", "ELSE", "FOR", "REPEAT", "WHEN"

        };

        // Returns true when the given identifier is a reserved word.
        public static bool IsReservedWord(string word)
            => ReservedWords.Contains(word);

        // Strips everything from %% onward on a single line.
        public static string StripComment(string line)
        {
            int idx = line.IndexOf("%%");
            return idx >= 0 ? line[..idx] : line;
        }

        // Tokenizes the full source into a list of
        // (1-based line number, cleaned content) pairs,
        // skipping blank lines and pure comment lines.
        public static List<(int LineNumber, string Content)> Tokenize(string source)
        {
            var result = new List<(int, string)>();
            string[] raw = source.Split('\n');

            for (int i = 0; i < raw.Length; i++)
            {
                string cleaned = StripComment(raw[i]).Trim();
                if (!string.IsNullOrWhiteSpace(cleaned))
                    result.Add((i + 1, cleaned));
            }

            return result;
        }
    }
}