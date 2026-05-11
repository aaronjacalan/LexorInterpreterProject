// Handles LEXOR `SCAN:` statements.
// Per spec, SCAN reads a single input line containing comma-separated values,
// then assigns them to the listed variables in order.

using System.Globalization;

namespace LexorInterpreter.ProgramCodes
{
    public static class Scanner
    {
        public static string? Execute(
            string line,
            int lineNumber,
            Dictionary<string, Variable> symbolTable)
        {
            if (!line.StartsWith("SCAN:"))
                return $"Line {lineNumber}: Malformed SCAN statement.";

            string rest = line["SCAN:".Length..].Trim();
            if (string.IsNullOrWhiteSpace(rest))
                return $"Line {lineNumber}: SCAN requires at least one variable name.";

            var targets = SplitTargets(rest);
            foreach (string rawName in targets)
            {
                string name = rawName.Trim();
                if (!symbolTable.ContainsKey(name))
                    return $"Line {lineNumber}: Undefined variable '{name}' in SCAN.";
            }

            string? input = Console.ReadLine();
            if (input == null) input = "";
            var values = SplitInputValues(input);

            if (values.Count != targets.Count)
                return $"Line {lineNumber}: SCAN expected {targets.Count} value(s) but received {values.Count}.";

            for (int i = 0; i < targets.Count; i++)
            {
                string name = targets[i].Trim();
                var variable = symbolTable[name];
                string rawVal = values[i].Trim();

                var (parsed, err) = ParseToType(rawVal, variable.DataType, lineNumber);
                if (err != null) return err;
                variable.Value = parsed;
            }

            return null;
        }

        // Splits "x, y, z" on commas, ignoring extra spaces.
        private static List<string> SplitTargets(string input)
        {
            return input.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        // Splits user input values on commas. Quotes are allowed for BOOL ("TRUE"/"FALSE").
        private static List<string> SplitInputValues(string input)
        {
            var parts = new List<string>();
            int start = 0;
            bool inStr = false;
            char strCh = '"';

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (!inStr && (c == '"' || c == '\'')) { inStr = true; strCh = c; }
                else if (inStr && c == strCh) { inStr = false; }
                else if (!inStr && c == ',')
                {
                    parts.Add(input[start..i]);
                    start = i + 1;
                }
            }
            parts.Add(input[start..]);

            return parts.Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        }

        private static (object? value, string? error) ParseToType(string raw, DataType type, int lineNumber)
        {
            switch (type)
            {
                case DataType.INT:
                    if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
                        return (iv, null);
                    return (null, $"Line {lineNumber}: '{raw}' is not a valid INT for SCAN.");

                case DataType.FLOAT:
                    if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float fv))
                        return (fv, null);
                    return (null, $"Line {lineNumber}: '{raw}' is not a valid FLOAT for SCAN.");

                case DataType.CHAR:
                    if (raw.Length == 1) return (raw[0], null);
                    if (raw.Length == 3 && raw[0] == '\'' && raw[2] == '\'') return (raw[1], null);
                    return (null, $"Line {lineNumber}: '{raw}' is not a valid CHAR for SCAN (enter a single character).");

                case DataType.BOOL:
                    string trimmed = raw.Trim();
                    if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
                    {
                        string b = trimmed[1..^1];
                        if (b == "TRUE") return (true, null);
                        if (b == "FALSE") return (false, null);
                        if (b is "true" or "false")
                            return (null, $"Line {lineNumber}: BOOL literals must be uppercase TRUE/FALSE and inside double quotes.");
                        return (null, $"Line {lineNumber}: '{raw}' is not a valid BOOL for SCAN (use \"TRUE\"/\"FALSE\").");
                    }
                    if (trimmed is "TRUE" or "FALSE" or "true" or "false")
                        return (null, $"Line {lineNumber}: BOOL literals must be in double quotes (\"TRUE\"/\"FALSE\").");
                    return (null, $"Line {lineNumber}: '{raw}' is not a valid BOOL for SCAN (use \"TRUE\"/\"FALSE\").");

                default:
                    return (null, $"Line {lineNumber}: Unsupported type for SCAN.");
            }
        }
    }
}

