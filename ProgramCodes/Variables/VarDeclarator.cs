using System.Collections.Generic;
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
            if (!Syntax.StartsWithKeyword(line, "DECLARE", out string rest))
                return $"Invalid DECLARE syntax.";

            int spaceIdx = rest.IndexOfAny(new[] { ' ', '\t' });
            if (spaceIdx < 0)
                return $"Invalid DECLARE syntax — missing type or variable name.";

            string typeName = rest[..spaceIdx].Trim().ToUpper();
            string varsPart = rest[spaceIdx..].Trim();

            if (!Enum.TryParse(typeName, out DataType dataType))
                return $"Unknown data type '{typeName}'.";

            foreach (string decl in SplitDeclarations(varsPart))
            {
                string trimmed = decl.Trim();
                string varName;
                object? initValue;

                if (TypeHelper.IsArrayType(dataType))
                {
                    int parenOpen = trimmed.IndexOf('(');
                    int parenClose = trimmed.IndexOf(')');
                    if (parenOpen < 0 || parenClose < 0 || parenClose != trimmed.Length - 1)
                        return $"Array declaration must use 'name(size)' syntax, e.g. arr(5).";

                    varName = trimmed[..parenOpen].Trim();
                    string sizeStr = trimmed[(parenOpen + 1)..parenClose].Trim();

                    if (!int.TryParse(sizeStr, out int arraySize) || arraySize < 1)
                        return $"Array size must be a positive integer, got '{sizeStr}'.";

                    if (!ValidName.IsMatch(varName))
                        return $"Invalid variable name '{varName}'.";
                    if (Lexer.IsReservedWord(varName))
                        return $"'{varName}' is a reserved word and cannot be a variable name.";
                    if (symbolTable.ContainsKey(varName))
                        return $"Variable '{varName}' is already declared.";

                    initValue = ArrayDefaultValue(dataType, arraySize);
                    symbolTable[varName] = new Variable(varName, dataType, initValue, true);
                    continue;
                }

                if (TypeHelper.IsStackType(dataType))
                {
                    varName = trimmed;
                    if (!ValidName.IsMatch(varName))
                        return $"Invalid variable name '{varName}'.";
                    if (Lexer.IsReservedWord(varName))
                        return $"'{varName}' is a reserved word and cannot be a variable name.";
                    if (symbolTable.ContainsKey(varName))
                        return $"Variable '{varName}' is already declared.";

                    symbolTable[varName] = new Variable(varName, dataType, new Stack<object>(), true);
                    continue;
                }

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

                if (!ValidName.IsMatch(varName))
                    return $"Invalid variable name '{varName}'.";

                if (Lexer.IsReservedWord(varName))
                    return $"'{varName}' is a reserved word and cannot be a variable name.";

                if (symbolTable.ContainsKey(varName))
                    return $"Variable '{varName}' is already declared.";

                symbolTable[varName] = new Variable(varName, dataType, initValue, hasInitValue);
            }

            return null;
        }

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
                    if (raw.Contains('.'))
                        return $"INT literals must not include a decimal point.";
                    if (int.TryParse(raw, out int iv)) { value = iv; return null; }
                    if (long.TryParse(raw, out _))
                        return $"INT literal out of range.";
                    return $"'{raw}' is not a valid INT.";

                case DataType.FLOAT:
                    if (float.TryParse(raw,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out float fv))
                    {
                        if (float.IsInfinity(fv) || float.IsNaN(fv))
                            return $"FLOAT literal out of range.";
                        if (fv == 0f && raw.StartsWith("-", StringComparison.Ordinal))
                            return $"-0.0 is not allowed.";
                        value = fv;
                        return null;
                    }
                    if (double.TryParse(raw,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out _))
                        return $"FLOAT literal out of range.";
                    return $"'{raw}' is not a valid FLOAT.";

                case DataType.CHAR:
                    if (raw.Length == 3 && raw[0] == '\'' && raw[2] == '\'')
                    { value = raw[1]; return null; }
                    return $"'{raw}' is not a valid CHAR — use 'c'.";

                case DataType.BOOL:
                    if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
                    {
                        string b = raw[1..^1];
                        if (b == "TRUE")  { value = true;  return null; }
                        if (b == "FALSE") { value = false; return null; }
                        if (b is "true" or "false")
                            return $"BOOL literals must be uppercase TRUE/FALSE and inside double quotes.";
                        return $"'{raw}' is not a valid BOOL — use \"TRUE\" or \"FALSE\".";
                    }
                    if (raw is "TRUE" or "FALSE" or "true" or "false")
                        return $"BOOL literals must be in double quotes (\"TRUE\"/\"FALSE\").";
                    return $"'{raw}' is not a valid BOOL — use \"TRUE\" or \"FALSE\".";

                case DataType.STRING:
                    if (raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"')
                    {
                        value = raw[1..^1];
                        return null;
                    }
                    return $"'{raw}' is not a valid STRING - use double quotes.";

                default:
                    return $"Unsupported type for literal parsing.";
            }
        }

        private static object? DefaultValue(DataType type) => type switch
        {
            DataType.INT   => (object)0,
            DataType.FLOAT => (object)0.0f,
            DataType.CHAR  => (object)'\0',
            DataType.BOOL  => (object)false,
            DataType.STRING => (object)string.Empty,
            _              => null
        };

        private static object? ArrayDefaultValue(DataType arrType, int size)
        {
            var elemType = TypeHelper.ElementType(arrType);
            var arr = new object[size];
            for (int i = 0; i < size; i++)
                arr[i] = DefaultValue(elemType)!;
            return arr;
        }
    }
}
