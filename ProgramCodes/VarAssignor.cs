// Executes assignment statements.
// - Supports chained assignment: `x = y = 4`
// - Supports literals (INT/FLOAT/CHAR/BOOL) and copying from another variable
// - Enforces type compatibility based on the target variable

namespace LexorInterpreter.ProgramCodes
{
    public static class VariableAssignor
    {
        public static string? Execute(
            string line,
            int lineNumber,
            Dictionary<string, Variable> symbolTable)
        {
            // Split "x = y = 4" into ["x", "y", "4"]
            var parts = SplitOnAssignment(line);
            if (parts.Count < 2)
                return $"Line {lineNumber}: Invalid assignment statement.";

            string rawValue  = parts[^1].Trim();
            var    targets   = parts[..^1];

            // Use the first target's type to parse the value
            string firstName = targets[0].Trim();
            if (!symbolTable.TryGetValue(firstName, out Variable? anchor))
                return $"Line {lineNumber}: Undefined variable '{firstName}'.";

            var (resolved, err) = ResolveValue(rawValue, anchor.DataType, lineNumber, symbolTable);
            if (err != null) return err;

            foreach (string t in targets)
            {
                string name = t.Trim();
                if (!symbolTable.TryGetValue(name, out Variable? variable))
                    return $"Line {lineNumber}: Undefined variable '{name}'.";

                variable.Value = resolved;
            }

            return null;
        }

        // Splits on bare '=' at top level (paren depth 0), ignoring '==', '<=', '>=', '<>'
        private static List<string> SplitOnAssignment(string line)
        {
            var  parts = new List<string>();
            int  start = 0;
            bool inStr = false;
            char strCh = '"';
            int  parenDepth = 0;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (!inStr && (c == '\'' || c == '"')) { inStr = true; strCh = c; }
                else if (inStr && c == strCh)          { inStr = false; }
                else if (!inStr && c == '(')           { parenDepth++; }
                else if (!inStr && c == ')')           { if (parenDepth > 0) parenDepth--; }
                else if (!inStr && parenDepth == 0 && c == '='
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

            // Reference to another variable
            if (symbolTable.TryGetValue(trimmed, out Variable? refVar))
            {
                if (refVar.DataType != expectedType)
                    return (null, $"Line {lineNumber}: Type mismatch — cannot assign {refVar.DataType} to {expectedType}.");
                return (refVar.Value, null);
            }

            // Try expression evaluation (supports unary, arithmetic, comparisons, AND/OR/NOT).
            // If the RHS is a simple literal, the evaluator will still accept it.
            var (value, actualType, exprErr) = ExpressionEvaluator.Evaluate(trimmed, lineNumber, symbolTable);
            if (exprErr != null) return (null, exprErr);

            // Enforce strong typing with one practical widening: INT -> FLOAT.
            if (expectedType == actualType) return (value, null);
            if (expectedType == DataType.FLOAT && actualType == DataType.INT)
                return ((float)(int)value!, null);

            return (null, $"Line {lineNumber}: Type mismatch — cannot assign {actualType} to {expectedType}.");
        }
    }
}
