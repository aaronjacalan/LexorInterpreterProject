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
                return $"Line {lineNumber}: Malformed SCAN statement";

            string rest = line["SCAN:".Length..].Trim();
            if (string.IsNullOrWhiteSpace(rest))
                return $"Line {lineNumber}: SCAN requires at least one variable name";

            var targets = SplitTargets(rest);
            foreach (string rawName in targets)
            {
                string name = rawName.Trim();
                if (!symbolTable.ContainsKey(name))
                    return $"Line {lineNumber}: Undefined variable '{name}' in SCAN";
            }

            string? input = Console.ReadLine();
            if (input == null) input = "";
            var values = SplitInputValues(input);

            if (values.Count != targets.Count)
                return $"Line {lineNumber}: SCAN expected {targets.Count} value(s) but received {values.Count}";

            for (int i = 0; i < targets.Count; i++)
            {
                string name = targets[i].Trim();
                var variable = symbolTable[name];
                string rawVal = values[i].Trim();

                var (parsed, err) = ParseToType(rawVal, variable.DataType, lineNumber);
                if (err != null) return err;
                variable.Value = parsed;
                variable.IsInitialized = true;
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
                    if (raw.Contains('.')) return (null, $"Line {lineNumber}: INTEGER {raw} must not include a decimal point");
                    if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iv))
                        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                            ? (null, $"Line {lineNumber}: INT out of range for SCAN")
                            : (null, $"Line {lineNumber}: '{raw}' is not a valid INT for SCAN");
                    return (iv, null);

                case DataType.FLOAT:
                    if (!raw.Contains('.')) return (null, $"Line {lineNumber}: 'FLOAT {raw}' must include a decimal point");
                    if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float fv))
                        return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                            ? (null, $"Line {lineNumber}: FLOAT literal out of range for SCAN")
                            : (null, $"Line {lineNumber}: '{raw}' is not a valid FLOAT for SCAN");
                    if (float.IsInfinity(fv) || float.IsNaN(fv)) return (null, $"Line {lineNumber}: FLOAT out of range for SCAN");
                    if (fv == 0f && raw.StartsWith("-", StringComparison.Ordinal)) return (null, $"Line {lineNumber}: -0.0 is not allowed for SCAN");
                    return (fv, null);

                case DataType.CHAR:
                    if (raw.Length != 1 && !(raw.Length == 3 && raw[0] == '\'' && raw[2] == '\''))
                        return (null, $"Line {lineNumber}: '{raw}' is not a valid CHAR for SCAN (only single characters)");
                    return (raw.Length == 1) ? (raw[0], null) : (raw[1], null);

                case DataType.BOOL:
                    string trimmed = raw.Trim();
                    if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
                    {
                        string b = trimmed[1..^1];
                        if (b == "TRUE") return (true, null);
                        if (b == "FALSE") return (false, null);
                        return b is "true" or "false"
                            ? (null, $"Line {lineNumber}: BOOL literals must be uppercase TRUE/FALSE (quotes optional)")
                            : (null, $"Line {lineNumber}: '{raw}' is not a valid BOOL for SCAN (use TRUE/FALSE)");
                    }
                    if (trimmed == "TRUE") return (true, null);
                    if (trimmed == "FALSE") return (false, null);
                    if (trimmed is "true" or "false")
                        return (null, $"Line {lineNumber}: BOOL literals must be uppercase TRUE/FALSE (quotes optional)");
                    return (null, $"Line {lineNumber}: '{raw}' is not a valid BOOL for SCAN (use TRUE/FALSE)");

                case DataType.STRING:
                    string stringValue = raw.Trim();
                    if (stringValue.Length >= 2 && stringValue[0] == '"' && stringValue[^1] == '"')
                        return (stringValue[1..^1], null);
                    if (stringValue.Length >= 2 && stringValue[0] == '\'' && stringValue[^1] == '\'')
                        return (null, $"Line {lineNumber}: STRING values for SCAN must use double quotes when quoted");
                    return (stringValue, null);

                default:
                    return (null, $"Line {lineNumber}: Unsupported type for SCAN");
            }
        }
    }
}

