# LEXOR Interpreter


## How it functions (high-level flow)
This project is a **pure interpreter** for the LEXOR language. It reads your source file, validates the required program structure, builds a symbol table from `DECLARE` statements, then executes the rest of the script line-by-line.

**Execution flow**

START
→ `Program.cs` (reads the LEXOR file into a source string)
→ `ProgramCodes/Lexer.cs` → `Lexer.Tokenize()` (strip `%%` comments, remove blanks, keep line numbers)
→ `ProgramCodes/Interpreter.cs` → `Interpreter.ValidateStructure()` (SCRIPT AREA / START SCRIPT / END SCRIPT)
→ `ProgramCodes/Interpreter.cs` → `Interpreter.ExtractBody()` (get lines between START/END)
→ `ProgramCodes/Interpreter.cs` → `Interpreter.FindDeclareBoundary()` (split DECLARE vs executable lines)
→ `ProgramCodes/VarDeclarator.cs` → `VariableDeclarator.Parse()` (build symbol table from DECLARE lines)
→ execute each remaining line:
  - `PRINT:` → `ProgramCodes/Printer.cs` → `Printer.Execute()`
  - `SCAN:` → `ProgramCodes/Scanner.cs` → `Scanner.Execute()`
  - assignment → `ProgramCodes/VarAssignor.cs` → `VariableAssignor.Execute()`
    → `ProgramCodes/ExpressionEvaluator.cs` (uses `ExpressionTokenizer.cs` + `ExpressionParser.cs` + `ExpressionOperations.cs`)
    → `ExpressionEvaluator.Evaluate()` → store result in `ProgramCodes/Variables.cs` (`Variable.Value`)
END

---

## Requirements
- .NET 9.0 SDK — download at https://dotnet.microsoft.com/download

---

## How to Build
After every edit to an internal file (any `.cs` files within `ProgramCodes`), Navigate to the project folder (Where the `.csproj` is located at) and run:
```bash
dotnet build
```
- You must rebuild (`dotnet build`) whenever you change any `.cs` file
- You **don't** need to rebuild when you only change `TestingCode`


---

## How to Run
Write the code inside `TestYourCodeHere` file, then run:
```bash
.\bin\Debug\net9.0\LexorInterpreterProject.exe .\TestingCode
```

---