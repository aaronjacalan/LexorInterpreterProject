// ============================================================
//  VariableDeclarator.cs
//  Parses DECLARE statements and populates the symbol table.
//
//  Syntax:
//    DECLARE <TYPE> <name>[=<value>] [, <name>[=<value>]]*
//
//  Rules:
//    - Variable names: start with letter or _, followed by letters/digits/_
//    - Names are case-sensitive
//    - Reserved words cannot be used as variable names
//    - All declarations must appear right after START SCRIPT
//    - Defaults: INT=0, FLOAT=0.0, CHAR='\0', BOOL=false
// ============================================================

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
            // Remove "DECLARE " prefix and read the type token
            string rest = line["DECLARE".Length..].Trim();
            int spaceIdx = rest.IndexOf(' ');
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

                if (trimmed.Contains('='))
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

                if (!ValidName.IsMatch(varName))
                    return $"Line {lineNumber}: Invalid variable name '{varName}'.";

                if (Lexer.IsReservedWord(varName))
                    return $"Line {lineNumber}: '{varName}' is a reserved word and cannot be a variable name.";

                if (symbolTable.ContainsKey(varName))
                    return $"Line {lineNumber}: Variable '{varName}' is already declared.";

                symbolTable[varName] = new Variable(varName, dataType, initValue);
            }

            return null;
        }

        // Splits "x, y=5, z" on commas while respecting quoted literals
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

        private static string? ParseLiteral(
            string raw, DataType type, int lineNum, out object? value)
        {
            value = null;
            switch (type)
            {
                case DataType.INT:
                    if (int.TryParse(raw, out int iv)) { value = iv; return null; }
                    return $"Line {lineNum}: '{raw}' is not a valid INT.";

                case DataType.FLOAT:
                    if (float.TryParse(raw,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float fv)) { value = fv; return null; }
                    return $"Line {lineNum}: '{raw}' is not a valid FLOAT.";

                case DataType.CHAR:
                    if (raw.Length == 3 && raw[0] == '\'' && raw[2] == '\'')
                    { value = raw[1]; return null; }
                    return $"Line {lineNum}: '{raw}' is not a valid CHAR — use 'c'.";

                case DataType.BOOL:
                    string b = raw.Trim('"').ToUpper();
                    if (b == "TRUE")  { value = true;  return null; }
                    if (b == "FALSE") { value = false; return null; }
                    return $"Line {lineNum}: '{raw}' is not a valid BOOL — use \"TRUE\" or \"FALSE\".";

                default:
                    return $"Line {lineNum}: Unsupported type for literal parsing.";
            }
        }

        private static object? DefaultValue(DataType type) => type switch
        {
            DataType.INT   => (object)0,
            DataType.FLOAT => (object)0.0f,
            DataType.CHAR  => (object)'\0',
            DataType.BOOL  => (object)false,
            _              => null
        };
    }
}