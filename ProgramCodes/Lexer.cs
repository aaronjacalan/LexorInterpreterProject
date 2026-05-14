// Preprocesses source input for the interpreter.
// - Removes `%%` comments and blank lines
// - Produces (lineNumber, cleanedLine) pairs
// - Holds the set of LEXOR reserved words

namespace LexorInterpreter.ProgramCodes
{
    public static class Lexer
    {
        // Returns true when the given identifier is a reserved word.
        public static bool IsReservedWord(string word)
            => LanguageDefinitions.ReservedWords.Contains(word);

        // Strips everything from %% onward on a single line.
        public static string StripComment(string line)
        {
            int idx = line.IndexOf("%%");
            return idx >= 0 ? line[..idx] : line;
        }

        // Tokenizes the source into (lineNumber, cleanedLine) pairs, skipping blanks/comments.
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
