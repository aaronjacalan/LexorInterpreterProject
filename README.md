# LEXOR Interpreter — README

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