// Executes assignment statements.
// - Supports chained assignment: `x = y = 4`
// - Supports literals (INT/FLOAT/CHAR/BOOL) and copying from another variable
// - Enforces type compatibility based on the target variable
// - Supports array element assignment: `arr[expr] = value`

namespace LexorInterpreter.ProgramCodes
{
    public static class VariableAssignor
    {
        public static string? Execute(
            string line,
            int lineNumber,
            Dictionary<string, Variable> symbolTable)
        {
            var parts = SplitOnAssignment(line);
            if (parts.Count < 2)
                return $"Invalid assignment statement.";

            string rawValue  = parts[^1].Trim();
            var    targets   = parts[..^1];

            string firstName = targets[0].Trim();

            // Array element write: arr[expr] = value
            var (arrName, indexExpr) = ParseArrayTarget(firstName);
            if (arrName != null)
            {
                if (!symbolTable.TryGetValue(arrName, out Variable? arrVar))
                    return $"Undefined array '{arrName}'.";
                if (!TypeHelper.IsArrayType(arrVar.DataType))
                    return $"'{arrName}' is not an array.";

                DataType elemType = TypeHelper.ElementType(arrVar.DataType);
                var (resolved, err) = ResolveValue(rawValue, elemType, lineNumber, symbolTable);
                if (err != null) return err;

                foreach (string t in targets)
                {
                    string target = t.Trim();
                    var (an, ie) = ParseArrayTarget(target);
                    if (an == null)
                        return $"Array assignment requires array element target, got '{target}'.";

                    if (!symbolTable.TryGetValue(an, out Variable? tv))
                        return $"Undefined array '{an}'.";

                    var (idxVal, idxType, idxErr) = ExpressionEvaluator.Evaluate(ie!, lineNumber, symbolTable);
                    if (idxErr != null) return idxErr;
                    if (idxType != DataType.INT)
                        return $"Array index must be INT, got {idxType}.";
                    int idx = (int)idxVal!;
                    var arr = (object[])tv.Value!;
                    if (idx < 0 || idx >= arr.Length)
                        return $"Array index {idx} out of bounds (size {arr.Length}).";
                    arr[idx] = resolved!;
                }
                return null;
            }

            if (!symbolTable.TryGetValue(firstName, out Variable? anchor))
                return $"Undefined variable '{firstName}'.";

            var (resolvedVal, resolveErr) = ResolveValue(rawValue, anchor.DataType, lineNumber, symbolTable);
            if (resolveErr != null) return resolveErr;

            foreach (string t in targets)
            {
                string name = t.Trim();
                if (!symbolTable.TryGetValue(name, out Variable? variable))
                    return $"Undefined variable '{name}'.";

                variable.Value = resolvedVal;
                variable.IsInitialized = true;
            }

            return null;
        }

        private static (string? arrName, string? indexExpr) ParseArrayTarget(string target)
        {
            int bracketOpen = target.IndexOf('[');
            if (bracketOpen <= 0) return (null, null);
            int bracketClose = target.LastIndexOf(']');
            if (bracketClose != target.Length - 1) return (null, null);

            string name = target[..bracketOpen].Trim();
            string idx  = target[(bracketOpen + 1)..bracketClose].Trim();

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(idx))
                return (null, null);

            return (name, idx);
        }

        private static List<string> SplitOnAssignment(string line)
        {
            var  parts = new List<string>();
            int  start = 0;
            bool inStr = false;
            char strCh = '"';
            int  parenDepth = 0;
            int  bracketDepth = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (!inStr && (c == '\'' || c == '"')) { inStr = true; strCh = c; }
                else if (inStr && c == strCh)          { inStr = false; }
                else if (!inStr && c == '(')           { parenDepth++; }
                else if (!inStr && c == ')')           { if (parenDepth > 0) parenDepth--; }
                else if (!inStr && c == '[')           { bracketDepth++; }
                else if (!inStr && c == ']')           { if (bracketDepth > 0) bracketDepth--; }
                else if (!inStr && parenDepth == 0 && bracketDepth == 0 && c == '='
                         && (i + 1 >= line.Length || line[i + 1] != '=')
                         && (i == 0 || (line[i - 1] != '<' && line[i - 1] != '>' && line[i - 1] != '!' && line[i - 1] != '=')))
                {
                    parts.Add(line[start..i]);
                    start = i + 1;
                }
            }
            parts.Add(line[start..]);
            return parts;
        }

        private static (object? value, string? error) ResolveValue(
            string raw,
            DataType expectedType,
            int lineNumber,
            Dictionary<string, Variable> symbolTable)
        {
            string trimmed = raw.Trim();

            // Array element read: arr[expr]
            var (arrName, indexExpr) = ParseArrayTarget(trimmed);
            if (arrName != null)
            {
                if (!symbolTable.TryGetValue(arrName, out Variable? arrVar))
                    return (null, $"Undefined array '{arrName}'.");
                if (!TypeHelper.IsArrayType(arrVar.DataType))
                    return (null, $"'{arrName}' is not an array.");

                var (idxVal, idxType, idxErr) = ExpressionEvaluator.Evaluate(indexExpr!, lineNumber, symbolTable);
                if (idxErr != null) return (null, idxErr);
                if (idxType != DataType.INT)
                    return (null, $"Array index must be INT, got {idxType}.");
                int idx = (int)idxVal!;
                var arr = (object[])arrVar.Value!;
                if (idx < 0 || idx >= arr.Length)
                    return (null, $"Array index {idx} out of bounds (size {arr.Length}).");

                DataType elemType = TypeHelper.ElementType(arrVar.DataType);
                object? elemValue = arr[idx];
                if (elemValue == null)
                    return (null, $"Array element '{arrName}[{idx}]' is uninitialized.");

                if (elemType != expectedType)
                {
                    if (expectedType == DataType.FLOAT && elemType == DataType.INT)
                        return ((float)(int)elemValue, null);
                    return (null, $"Type mismatch — cannot assign {elemType} to {expectedType}.");
                }

                return (elemValue, null);
            }

            if (symbolTable.TryGetValue(trimmed, out Variable? refVar))
            {
                if (refVar.DataType != expectedType)
                    return (null, $"Type mismatch — cannot assign {refVar.DataType} to {expectedType}.");
                return (refVar.Value, null);
            }

            var (value, actualType, exprErr) = ExpressionEvaluator.Evaluate(trimmed, lineNumber, symbolTable);
            if (exprErr != null) return (null, exprErr);

            if (expectedType == actualType) return (value, null);

            if (expectedType == DataType.FLOAT && actualType == DataType.INT)
                return ((float)(int)value!, null);

            return (null, $"Type mismatch — cannot assign {actualType} to {expectedType}.");
        }
    }
}
