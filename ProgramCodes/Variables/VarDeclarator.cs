// Parses `DECLARE` statements and creates variables.
// - Supports multiple declarations per line (comma-separated)
// - Allows optional initial values, otherwise uses type defaults
// - Rejects invalid names, duplicates, and reserved words

using System.Text.RegularExpressions;

namespace LexorInterpreter.ProgramCodes
{
    public static class VariableDeclarator
    {
        private static readonly Regex ValidName = new(@"^[a-zA-Z_][a-zA-Z0-9_]*$");

        public static string? Parse(
            string line,
            int lineNumber,
            Dictionary<string, Variable> symbolTable)
        {
            // Remove "DECLARE" prefix and read the type token.
            if (!Syntax.StartsWithKeyword(line, "DECLARE", out string rest))
                return $"Line {lineNumber}: Invalid DECLARE syntax.";

            int spaceIdx = rest.IndexOfAny(new[] { ' ', '\t' });
            if (spaceIdx < 0)
                return $"Line {lineNumber}: Invalid DECLARE syntax — missing type or variable name.";

            string typeName = rest[..spaceIdx].Trim().ToUpper();
            string varsPart = rest[spaceIdx..].Trim();

            if (!Enum.TryParse(typeName, out DataType dataType))
                return $"Line {lineNumber}: Unknown data type '{typeName}'.";

            foreach (string decl in SplitDeclarations(varsPart))
            {
                string trimmed = decl.Trim();
                string varName;
                object? initValue;

                bool hasInitValue = trimmed.Contains('=');
                if (hasInitValue)
                {
                    int eq = trimmed.IndexOf('=');
                    varName = trimmed[..eq].Trim();
                    string rawVal = trimmed[(eq + 1)..].Trim();
                    string? err = ParseLiteral(rawVal, dataType, lineNumber, out initValue);
                    if (err != null) return err;
                }
                else
                {
                    varName   = trimmed;
                    initValue = DefaultValue(dataType);
                }

                // Validate the variable name.
                if (!ValidName.IsMatch(varName))
                    return $"Line {lineNumber}: Invalid variable name '{varName}'.";

                // Validate the variable name is not a reserved word.
                if (Lexer.IsReservedWord(varName))
                    return $"Line {lineNumber}: '{varName}' is a reserved word and cannot be a variable name.";

                // Validate the variable name is not already declared.
                if (symbolTable.ContainsKey(varName))
                    return $"Line {lineNumber}: Variable '{varName}' is already declared.";

                symbolTable[varName] = new Variable(varName, dataType, initValue, hasInitValue);
            }

            return null;
        }

        // Splits "x, y=5, z" on commas while respecting quoted literals.
        private static List<string> SplitDeclarations(string input)
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
                else if (!inStr && c == ',')
                {
                    parts.Add(input[start..i]);
                    start = i + 1;
                }
            }
            parts.Add(input[start..]);
            return parts;
        }

        // Parses a literal value into the appropriate type.
        private static string? ParseLiteral(
            string raw, DataType type, int lineNum, out object? value)
        {
            value = null;
            switch (type)
            {
                case DataType.INT:
                    if (raw.Contains('.'))
                        return $"Line {lineNum}: INT literals must not include a decimal point.";
                    if (int.TryParse(raw, out int iv)) { value = iv; return null; }
                    if (long.TryParse(raw, out _))
                        return $"Line {lineNum}: INT literal out of range.";
                    return $"Line {lineNum}: '{raw}' is not a valid INT.";

                case DataType.FLOAT:
                    if (!raw.Contains('.'))
                        return $"Line {lineNum}: FLOAT literals must include a decimal point.";
                    if (float.TryParse(raw,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float fv))
                    {
                        if (float.IsInfinity(fv) || float.IsNaN(fv))
                            return $"Line {lineNum}: FLOAT literal out of range.";
                        if (fv == 0f && raw.StartsWith("-", StringComparison.Ordinal))
                            return $"Line {lineNum}: -0.0 is not allowed.";
                        value = fv;
                        return null;
                    }
                    if (double.TryParse(raw,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out _))
                        return $"Line {lineNum}: FLOAT literal out of range.";
                    return $"Line {lineNum}: '{raw}' is not a valid FLOAT.";

                case DataType.CHAR:
                    if (raw.Length == 3 && raw[0] == '\'' && raw[2] == '\'')
                    { value = raw[1]; return null; }
                    return $"Line {lineNum}: '{raw}' is not a valid CHAR — use 'c'.";

                case DataType.BOOL:
                    if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
                    {
                        string b = raw[1..^1];
                        if (b == "TRUE")  { value = true;  return null; }
                        if (b == "FALSE") { value = false; return null; }
                        if (b is "true" or "false")
                            return $"Line {lineNum}: BOOL literals must be uppercase TRUE/FALSE and inside double quotes.";
                        return $"Line {lineNum}: '{raw}' is not a valid BOOL — use \"TRUE\" or \"FALSE\".";
                    }
                    if (raw is "TRUE" or "FALSE" or "true" or "false")
                        return $"Line {lineNum}: BOOL literals must be in double quotes (\"TRUE\"/\"FALSE\").";
                    return $"Line {lineNum}: '{raw}' is not a valid BOOL — use \"TRUE\" or \"FALSE\".";

                case DataType.STRING:
                    if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
                    {
                        value = raw[1..^1];
                        return null;
                    }
                    return $"Line {lineNum}: '{raw}' is not a valid STRING - use double quotes.";

                default:
                    return $"Line {lineNum}: Unsupported type for literal parsing.";
            }
        }

        // Returns the default value for a given data type.
        private static object? DefaultValue(DataType type) => type switch
        {
            DataType.INT   => (object)0,
            DataType.FLOAT => (object)0.0f,
            DataType.CHAR  => (object)'\0',
            DataType.BOOL  => (object)false,
            DataType.STRING => (object)string.Empty,
            _              => null
        };
    }
}
