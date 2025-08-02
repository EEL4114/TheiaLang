using System.Diagnostics;
using TheiaLang;

const ConsoleColor LEXER_COL = ConsoleColor.Cyan;
const ConsoleColor PARSER_COL = ConsoleColor.Yellow;
const ConsoleColor SEM_COL = ConsoleColor.DarkRed;
const ConsoleColor IRGEN_COL = ConsoleColor.Green;
const ConsoleColor LLVM_COL = ConsoleColor.Magenta;

Stopwatch compileTimer = new Stopwatch();
Stopwatch lexTimer = new Stopwatch();
Stopwatch parseTimer = new Stopwatch();
Stopwatch printTimer = new Stopwatch();
Stopwatch analysisTimer = new Stopwatch();
Stopwatch IRGenTimer = new Stopwatch();
Stopwatch LLVMTimer = new Stopwatch();


List<string> programNames = [];
if (args.Length == 0)
{
    Log.Usage();
    return;
}

if (args[0].EndsWith(".tia"))
{
    if (!File.Exists(args[0]))
        Log.Error(15, $"File '{args[0]}' could not be found");

    programNames = [args[0][0..args[0].IndexOf('.')]];
    CompileFile(programNames[0]);
}
else    // folder
{
    if (args[0].StartsWith("--test") && args.Length > 1)
    {
        string folder = args[1];
        if (!Directory.Exists(folder))
            Log.Error(15, $"Folder '{folder}' could not be found");

        IOrderedEnumerable<string> allTia = Directory.EnumerateFiles(folder, "*.tia", SearchOption.TopDirectoryOnly)
                                                .OrderBy(f => f);
        int failures = 0;
        foreach (string tiaFile in allTia)
        {
            Log.Info($"=== Testing {Path.GetFileName(tiaFile)} ===");

            try { CompileFile(tiaFile[0..tiaFile.IndexOf('.')]); }
            catch (Exception ex)
            {
                Log.Error(99, ex.Message);
                failures++;
            }
        }
        Log.Info($"\n{allTia.Count()} files tested, {failures} failures.");
    }
    else
    {
        Log.Error(16, $"Invalid argument: '{args[0]}'", false);
        Log.Usage();
        return;
    }
}

int CompileFile(string programName)
{
    string code = File.ReadAllText(programName + ".tia");

    compileTimer.Start();
    lexTimer.Start();
    Lexer lexer = new Lexer(code);
    List<Token> tokens = [];
    Token token;

    do
    {
        token = lexer.NextToken();
        tokens.Add(token);
        // Log.Info(token.ToString());
        // Log.Info(token.TokenType.ToString());
    } while (token.TokenType != TokenType.EOF);

    lexTimer.Stop();
    parseTimer.Start();

    Parser parser = new Parser(tokens);

    (ProgramNode ast, Scope globalScope) = parser.ParseProgram(programName[(1 + programName.LastIndexOf('\\'))..]);

    parseTimer.Stop();
    compileTimer.Stop();
    printTimer.Start();

    using StreamWriter writer = new StreamWriter($"{programName}.ast");
    AstPrinter.Print(ast, writer);

    printTimer.Stop();
    compileTimer.Start();
    analysisTimer.Start();

    (ast, globalScope) = SemanticAnalyser.AnalyseProgram(ast, globalScope);

    analysisTimer.Stop();
    compileTimer.Stop();
    printTimer.Start();

    using StreamWriter writer2 = new StreamWriter($"{programName}_full.ast");
    AstPrinter.Print(ast, writer2);

    compileTimer.Start();
    IRGenTimer.Start();

    IRGenerator_old.Emit(ast, globalScope, $"{programName}.ll");

    IRGenTimer.Stop();
    LLVMTimer.Start();

    Process.Start(@"C:\Program Files\LLVM\bin\clang.exe", $"-x ir {programName}.ll -O0 -o {programName}.exe")?.WaitForExit();

    LLVMTimer.Stop();
    compileTimer.Stop();

    return 0;
}

int lexerTime = (int)lexTimer.Elapsed.TotalMilliseconds;
int parserTime = (int)parseTimer.Elapsed.TotalMilliseconds;
int semTime = (int)analysisTimer.Elapsed.TotalMilliseconds;
int IRgenTime = (int)IRGenTimer.Elapsed.TotalMilliseconds;
int LLVMTime = (int)LLVMTimer.Elapsed.TotalMilliseconds;

Log.Time("Lexer took", lexerTime, LEXER_COL);
Log.Time("Parser took", parserTime, PARSER_COL);
Log.Time("Semantic Analysis took", semTime, SEM_COL);
Console.WriteLine($"AST printing took {printTimer.ElapsedMilliseconds} ms");
Log.Time("IR Generation took", IRgenTime, IRGEN_COL);
Log.Time("LLVM took", LLVMTime, LLVM_COL);

Console.WriteLine($"All Processes finished in {compileTimer.ElapsedMilliseconds} ms");

double totalTime = lexerTime + parserTime + IRgenTime + LLVMTime;

#region Chart Printing

int lexerChars = (int)Math.Round(lexerTime / (double)totalTime * 50);
int parserChars = (int)Math.Round(parserTime / (double)totalTime * 50);
int semChars = (int)Math.Round(semTime / (double)totalTime * 50);
int IRgenChars = (int)Math.Round(IRgenTime / (double)totalTime * 50);
int llvmChars = 50 - lexerChars - parserChars - IRgenChars;

static void PrintSegment(int count, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.Write(new string('■', count));
    Console.ResetColor();
}

Console.Write("[");
PrintSegment(lexerChars, LEXER_COL);
PrintSegment(parserChars, PARSER_COL);
PrintSegment(semChars, SEM_COL);
PrintSegment(IRgenChars, IRGEN_COL);
PrintSegment(llvmChars, LLVM_COL);
Console.WriteLine("]");

#endregion

#region Log

public static class Log
{
    public static void Error(uint code, string message, bool exit = true)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.Write($"Error #{code}: ");
        Console.ResetColor();
        Console.Error.WriteLine(message);
        if (exit)
            Environment.Exit(1);
    }

    public static void Info(string text)
    {
        Console.WriteLine(text);
    }

    public static void Time(string text, int time, ConsoleColor highlight = ConsoleColor.White, float linesPerS = 0)
    {
        Console.Write($"{text} ");
        Console.ForegroundColor = highlight;
        Console.Write($"{time} ms ");
        Console.ResetColor();
        if (linesPerS > 0)
            Console.WriteLine($"  -  {linesPerS} lines/s");
        else
            Console.WriteLine();
    }

    public static void Usage()
    {
        Info("Usage: ");
        Info("  <path>.tia                       compile the specified file");
        Info("  --test <path>                    test all files in the specified folder");
    }
}

#endregion