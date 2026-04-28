# LEXOR Interpreter


## How it works
This project is a **pure interpreter** for the LEXOR language. It reads your source file, validates the required program structure, builds a symbol table from `DECLARE` statements, then executes the rest of the script line-by-line.

**Execution flow**

START
→ `Program.cs` (load file)
→ `ProgramCodes/Lexer.cs` (tokenize + strip `%%` comments)
→ `ProgramCodes/Interpreter.cs` (validate structure + split DECLARE vs executable)
→ `ProgramCodes/VarDeclarator.cs` (build symbol table)
→ `ProgramCodes/Interpreter.cs` executes each line:
  - `PRINT:` → `ProgramCodes/Printer.cs`
  - `SCAN:` → `ProgramCodes/Scanner.cs`
  - assignment → `ProgramCodes/VarAssignor.cs` → expression eval (`ProgramCodes/Expression*.cs`)
END


## Requirements
- .NET 9.0 SDK — download at https://dotnet.microsoft.com/download


## How to Build
After every edit to an internal file (any `.cs` files within `ProgramCodes`), Navigate to the project folder (Where the `.csproj` is located at) and run:
```bash
dotnet build
```
- You must rebuild (`dotnet build`) whenever you change any `.cs` file
- You **don't** need to rebuild when you only change `TestingCode`



## How to Run
Write the code inside `TestYourCodeHere` file, then run:
```bash
.\bin\Debug\net9.0\LexorInterpreterProject.exe .\TestingCode
```