// Handles LEXOR `PRINT:` statements.
// - Supports `&` concatenation
// - Prints literals, escape tokens like `[x]`, `$` newline, and variable values
// - Errors on unknown/undefined tokens

namespace LexorInterpreter.ProgramCodes
{
    public static class Printer
    {
        public static string? Execute(
            string line,
            int lineNumber,
            Dictionary<string, Variable> symbolTable)
        {
            if (!Syntax.TryReadCommand(line, "PRINT", out string rest))
                return $"Line {lineNumber}: Malformed PRINT statement.";

            var output  = new System.Text.StringBuilder();

            foreach (string token in SplitByConcatenator(rest))
            {
                string t = token.Trim();

                if (t == "$")
                {
                    output.Append('\n');
                }
                else if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    // Escape sequence: [#] prints #, [[] prints [, []] prints ]
                    output.Append(t[1..^1]);
                }
                else if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
                {
                    output.Append(t[1..^1]);
                }
                else if (t.StartsWith("'") && t.EndsWith("'") && t.Length == 3)
                {
                    output.Append(t[1]);
                }
                else if (symbolTable.TryGetValue(t, out Variable? variable))
                {
                    output.Append(variable.GetDisplayValue());
                }
                else
                {
                    var evaluated = ExpressionEvaluator.Evaluate(t, lineNumber, symbolTable);
                    if (evaluated.error != null) return evaluated.error;
                    output.Append(FormatValue(evaluated.value, evaluated.type));
                }
            }

            Console.Write(output.ToString());
            return null;
        }

        private static string FormatValue(object? value, DataType type)
        {
            if (value == null) return "";
            return type switch
            {
                DataType.BOOL => (bool)value ? "TRUE" : "FALSE",
                _ => value.ToString()!
            };
        }

        // Splits on '&' while respecting quoted strings.
        private static List<string> SplitByConcatenator(string input)
        {
            var  parts = new List<string>();
            int  start = 0;
            bool inStr = false;
            char strCh = '"';

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (!inStr && (c == '"' || c == '\'')) { inStr = true; strCh = c; }
                else if (inStr && c == strCh)          { inStr = false; }
                else if (!inStr && c == '&')
                {
                    parts.Add(input[start..i]);
                    start = i + 1;
                }
            }
            parts.Add(input[start..]);
            return parts;
        }
    }
}
