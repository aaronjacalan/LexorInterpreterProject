# LEXOR Interpreter — README

---

## Requirements
- .NET 9.0 SDK — download at https://dotnet.microsoft.com/download

---

## How to Build
Open PowerShell, navigate to the project folder, and run:
```bash
cd D:\CS322_LexorInterpreter_JacalanOng\LexorInterpreterProject
dotnet build
```

---

## How to Run
Write your LEXOR code inside `TestYourCodeHere.txt`, then run:
```bash
.\bin\Debug\net9.0\LexorInterpreterProject.exe D:\CS322_LexorInterpreter_JacalanOng\LexorInterpreterProject\TestYourCodeHere.txt
```

---

## Notes
- You must rebuild (`dotnet build`) whenever you change any `.cs` file
- You do **not** need to rebuild when you only change `TestYourCodeHere.txt`