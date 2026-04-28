# LEXOR Interpreter — README

---

## How it functions (high-level flow)
This project is a **pure interpreter** for the LEXOR language. It reads your source file, validates the required program structure, builds a symbol table from `DECLARE` statements, then executes the rest of the script line-by-line.

**Execution flow**

`Source file`  
→ `Lexer.Tokenize()` (strip `%%` comments, remove blanks, keep line numbers)  
→ `Interpreter.ValidateStructure()` (SCRIPT AREA / START SCRIPT / END SCRIPT)  
→ `Interpreter.ExtractBody()` (lines between START/END)  
→ `Interpreter.FindDeclareBoundary()`  
→ `DECLARE ...` lines  
→ `VariableDeclarator.Parse()` (create variables in the symbol table)  
→ executable lines  
→ for each line:
  - `PRINT:` → `Printer.Execute()` (handles `&`, `$`, escapes like `[[]`, `[]]`, `[#]`)
  - `SCAN:` → `Scanner.Execute()` (reads one comma-separated input line and assigns to variables)
  - assignment (`x = ...`) → `VariableAssignor.Execute()`
    → `ExpressionEvaluator.Evaluate()` (parentheses, unary `+/-/NOT`, arithmetic, comparisons, AND/OR)
    → type-check and store result in the symbol table

---

## Requirements
- .NET 9.0 SDK — download at https://dotnet.microsoft.com/download

---

## How to Build
Navigate to the project folder (Where the `.csproj` is placed) and run:
```bash
dotnet build
```

---

## How to Run
Write your LEXOR code inside `TestYourCodeHere` file, then run:
```bash
.\bin\Debug\net9.0\LexorInterpreterProject.exe .\TestingCode
```

---

## Notes
- You must rebuild (`dotnet build`) whenever you change any `.cs` file
- You do **not** need to rebuild when you only change `TestingCode`