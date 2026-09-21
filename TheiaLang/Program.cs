using System.Diagnostics;
using TheiaLang;
using static TimerGroup;

const ConsoleColor PRELOAD_COL = ConsoleColor.DarkGray;
const ConsoleColor LEXER_COL   = ConsoleColor.Cyan;
const ConsoleColor PARSER_COL  = ConsoleColor.Yellow;
const ConsoleColor ELAB_COL    = ConsoleColor.Blue;
const ConsoleColor SEM_COL     = ConsoleColor.DarkRed;
const ConsoleColor IRGEN_COL   = ConsoleColor.Green;
const ConsoleColor LLVM_COL    = ConsoleColor.Magenta;

const int BAR_CHARS = 100;

Dictionary<TimerGroup, Stopwatch> timers = [];

foreach(TimerGroup group in Enum.GetValues<TimerGroup>())
    timers.Add(group, new Stopwatch());

#region  Startup

timers[TG_Preload].Start();

const string OUTPUT_PATH  = "Output/";
const string DEBUG_PATH  = "Debug/";

string PRELOAD_PATH_REL = Path.Combine(AppContext.BaseDirectory, "__preload.tia");
string OUTPUT_PATH_REL  = Path.Combine(AppContext.BaseDirectory, OUTPUT_PATH);
string DEBUG_PATH_REL   = Path.Combine(AppContext.BaseDirectory, DEBUG_PATH);

Directory.CreateDirectory(Path.GetDirectoryName(OUTPUT_PATH_REL)!);
Directory.CreateDirectory(Path.GetDirectoryName(DEBUG_PATH_REL)!);

if (!File.Exists(PRELOAD_PATH_REL))
    Log.Error(18, "Preload module could not be located");

string preloadCode = File.ReadAllText(PRELOAD_PATH_REL);
Lexer preloadLexer = new Lexer(preloadCode);
List<Token> preloadTokens = [];
Token token;

do
{
    token = preloadLexer.NextToken();
    preloadTokens.Add(token);
    // Log.Info(token.ToString());
    // Log.Info(token.TokenType.ToString());
} while (token.TokenType != TokenType.EOF);

Parser preloadParser = new Parser(preloadTokens);
(ProgramNode preloadAST, Scope preloadScope) = preloadParser.ParseProgram("__preload__");

#if PRINT_PRELOAD
    using StreamWriter writer = new StreamWriter($"{DEBUG_PATH_REL}__preload__.ast");
    AstPrinter.Print(preloadAST, writer, false);
    using StreamWriter scopeWriter = new StreamWriter($"{DEBUG_PATH_REL}__preload__.scope");
    ScopePrinter.Print("__preload", preloadScope, scopeWriter);
#endif

timers[TG_Preload].Stop();

Log.Time("Preload took", timers[TG_Preload].Elapsed.TotalMilliseconds, PRELOAD_COL);

#endregion

#region Args

if (args.Length == 0)
{
    Log.Usage();
    return;
}

if (args[0].EndsWith(".tia"))
{
    if (!Path.Exists(args[0]) || !File.Exists(args[0]))
        Log.Error(15, $"File '{args[0]}' could not be found");

    long allocationsBefore = GC.GetTotalAllocatedBytes(false);

    CompileFile(args[0][0..args[0].LastIndexOf('.')], writeAST: true, writeScope: true);

    long allocated = GC.GetTotalAllocatedBytes(false) - allocationsBefore;

    Log.Info($"Allocated:   {FormatBytes(allocated)}");
    LogMemory();
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
            try
            {
                CompileFile(tiaFile[0..tiaFile.IndexOf('.')], insertLogs: false, timestamps: false);
            }
            catch (Exception ex)
            {
                failures++;
                Log.Error(17, ex.Message, false);
            }

            Log.Info($"=== Testing {Path.GetFileName(tiaFile)} ===");
        }

        // LogPeakMemory();

        Log.Info($"\n{allTia.Count()} files tested, {failures} failures.");
    }
    else
    {
        Log.Error(16, $"Invalid argument: '{args[0]}'", false);
        Log.Usage();
        return;
    }
}

#endregion

#region  Compilation

int CompileFile(string filePath,
                bool insertLogs = false,
                bool timestamps = true,
                bool writeLexerOutput = false,
                bool writeAST = false,
                bool writeScope = false)
{
    string programName = filePath[(1 + filePath.LastIndexOf('\\'))..];

    string F_OUTPUT_PATH_REL = Path.Combine(OUTPUT_PATH_REL, programName);
    string F_DEBUG_PATH_REL  = Path.Combine(DEBUG_PATH_REL, programName);

    Directory.CreateDirectory(F_OUTPUT_PATH_REL);
    Directory.CreateDirectory(F_DEBUG_PATH_REL);

    string code = File.ReadAllText(filePath + ".tia");

    foreach(TimerGroup group in Enum.GetValues<TimerGroup>())
        timers[group].Stop();

    timers[TG_Compile].Start();
    timers[TG_Lexer].Start();
    Lexer lexer = new Lexer(code);
    List<Token> tokens = [];
    Token token;

    string lexPath = $"{F_DEBUG_PATH_REL}{programName}.lex";
    using StreamWriter lexerWriter = new StreamWriter(lexPath);

    do {
        token = lexer.NextToken();
        tokens.Add(token);

        lexerWriter.Write($"{token.TokenType} ");
        if (token.TokenType == TokenType.Punctuation_Semicolon)
            lexerWriter.WriteLine();

    } while (token.TokenType != TokenType.EOF);

    Log.Link(lexPath, "Lexer tokens: ");

    timers[TG_Lexer].Stop();
    timers[TG_Parser].Start();

    Parser parser = new Parser(tokens);

    (ProgramNode ast, Scope globalScope) = parser.ParseProgram(programName);

    timers[TG_Parser].Stop();
    timers[TG_Elab].Start();
    (ast, globalScope) = new Elaboration().Lower(preloadScope, preloadAST, globalScope, ast);
    timers[TG_Elab].Stop();

    timers[TG_Print].Start();
    if (writeAST)
    {

        string writerPath = Path.Combine(F_DEBUG_PATH_REL, $"{programName}.ast");
        using StreamWriter writer = new StreamWriter(writerPath);
        AstPrinter.Print(ast, writer, timestamps);
        Log.Link(writerPath, "Preload AST: ");
    }
    if(writeScope)
    {
        string sw2Path = Path.Combine(F_DEBUG_PATH_REL, $"{programName}.scope");
        using StreamWriter sw2 = new StreamWriter(sw2Path);
        ScopePrinter.Print(filePath, globalScope, sw2);
        Log.Link(sw2Path, "Scope Tree: ");
    }

    timers[TG_Print].Stop();

    timers[TG_Analyser].Start();

    (ast, globalScope) = new SemanticAnalyser().AnalyseProgram(ast, globalScope);

    timers[TG_Analyser].Stop();
    timers[TG_Print].Start();

    if(writeScope)
    {
        string sw3Path = Path.Combine(F_DEBUG_PATH_REL, $"{programName}_full.scope");
        using StreamWriter sw3 = new StreamWriter(sw3Path);
        ScopePrinter.Print(filePath, globalScope, sw3);
        Log.Link(sw3Path, "Full Scope Tree: ");
    }
    if(writeAST)
    {
        string writer2Path = Path.Combine(F_DEBUG_PATH_REL, $"{programName}_full.ast");
        using StreamWriter writer2 = new StreamWriter(writer2Path);
        AstPrinter.Print(ast, writer2, timestamps);
        Log.Link(writer2Path, "Full AST: ");
    }

    timers[TG_Print].Stop();
    timers[TG_Layout].Start();

    LayoutCalc layout = new LayoutCalc(CompilationTarget.x86_64_windows);
    layout.GenerateLayout(ast);

    timers[TG_Layout].Stop();
    timers[TG_ConstFold].Start();

    ConstantFolding cf = new ConstantFolding(layout);
    cf.ConstFold(ast);

    timers[TG_ConstFold].Stop();

    timers[TG_IRGen].Start();

    string irgenPath = Path.Combine(F_OUTPUT_PATH_REL, $"{programName}.ll");
    new IRGenerator().Emit(ast, globalScope, irgenPath, layout, insertLogs);
    Log.Link(irgenPath, "IR: ");

    timers[TG_IRGen].Stop();
    timers[TG_LLVM].Start();

    ProcessStartInfo psi = new ProcessStartInfo
    {
        FileName = @"C:\Program Files\LLVM\bin\clang.exe",
        Arguments = $"-x ir {irgenPath} -O0 -rtlib=compiler-rt -o {F_OUTPUT_PATH_REL}{programName}.exe",

        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
    };

    string asmPath = Path.Combine(F_OUTPUT_PATH_REL, $"{programName}.s");
    ProcessStartInfo psi2 = new ProcessStartInfo
    {
        FileName = @"C:\Program Files\LLVM\bin\clang.exe",
        Arguments = $"-x ir {irgenPath} -march=native -O3 -S -o " + asmPath,

        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
    };

    Log.Link(asmPath, "ASM: ");


    using (Process process = Process.Start(psi)!)
    {
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
            throw new Exception(stderr);
    }

    using (Process process2 = Process.Start(psi2)!)
    {
        string stdout2 = process2.StandardOutput.ReadToEnd();
        string stderr2 = process2.StandardError.ReadToEnd();
        process2.WaitForExit();

        if (process2.ExitCode != 0)
            throw new Exception(stderr2);
    }

    timers[TG_LLVM].Stop();
    timers[TG_Compile].Stop();
    return 0;
}

#endregion

Dictionary<TimerGroup, double> times = [];

foreach(TimerGroup group in Enum.GetValues<TimerGroup>())
    times.Add(group, timers[group].Elapsed.TotalMilliseconds);

Log.Time("Lexer took",              times[TG_Lexer],    LEXER_COL);
Log.Time("Parser took",             times[TG_Parser],   PARSER_COL);
Log.Time("Semantic Analysis took",  times[TG_Analyser], SEM_COL);
Log.Time("AST printing took",       times[TG_Print],    ConsoleColor.White);
Log.Time("IR Generation took",      times[TG_IRGen],    IRGEN_COL);
Log.Time("LLVM took",               times[TG_LLVM],     LLVM_COL);

double miscTime = Math.Round(times[TG_Layout] + times[TG_ConstFold] + times[TG_Elab], 4);
Log.Time("Misc", miscTime, ConsoleColor.Gray);

Console.WriteLine($"All Processes finished in {times[TG_Compile]} ms");

double totalTime = times.Values.Sum() - times[TG_Compile];

#region Chart Printing

int preloadChars = (int)Math.Round(times[TG_Preload]  / (double)totalTime * BAR_CHARS);
int lexerChars   = (int)Math.Round(times[TG_Lexer]    / (double)totalTime * BAR_CHARS);
int parserChars  = (int)Math.Round(times[TG_Parser]   / (double)totalTime * BAR_CHARS);
int semChars     = (int)Math.Round(times[TG_Analyser] / (double)totalTime * BAR_CHARS);
int IRgenChars   = (int)Math.Round(times[TG_IRGen]    / (double)totalTime * BAR_CHARS);
int miscChars    = (int)Math.Round(miscTime           / (double)totalTime * BAR_CHARS);
int llvmChars    = BAR_CHARS - lexerChars - parserChars - IRgenChars;

static void PrintSegment(int count, ConsoleColor color)
{
    Console.ForegroundColor = color;
    Console.Write(new string('■', count));
    Console.ResetColor();
}

Console.Write("[");
PrintSegment(preloadChars,  PRELOAD_COL);
PrintSegment(lexerChars,    LEXER_COL);
PrintSegment(parserChars,   PARSER_COL);
PrintSegment(semChars,      SEM_COL);
PrintSegment(IRgenChars,    IRGEN_COL);
PrintSegment(miscChars,     ConsoleColor.Gray);
PrintSegment(llvmChars,     LLVM_COL);
Console.WriteLine("]");

#endregion

#region Log



static string FormatBytes(long bytes)
{
    const double MiB = 1024.0 * 1024.0;
    return $"{bytes / MiB:F2} MiB";
}


static void LogMemory()
{
    using Process p = Process.GetCurrentProcess();
    p.Refresh();

    Console.WriteLine($"Working set: {FormatBytes(p.WorkingSet64)}");
    Console.WriteLine($"Private:     {FormatBytes(p.PrivateMemorySize64)}");
    // Console.WriteLine($"Virtual:     {FormatBytes(p.VirtualMemorySize64)}");
}

static void LogPeakMemory()
{
    using Process p = Process.GetCurrentProcess();
    p.Refresh();

    Console.WriteLine($"Working set: {FormatBytes(p.PeakWorkingSet64)} (peak)");
    // Console.WriteLine($"Virtual:     {FormatBytes(p.PeakVirtualMemorySize64)} (peak)");
}

public static class Log
{
    public static void Error(uint code, string message, bool exit = true)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.Write($"Error #{code}: ");
        Console.ResetColor();
        StackTrace stackTrace = new StackTrace();
        throw new Exception(message);
    }

    public static void Info(string text)
    {
        Console.WriteLine(text);
    }

    public static void Time(string text, double time, ConsoleColor highlight = ConsoleColor.White, float linesPerS = 0)
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

    public static void Link(string path, string label = "")
    {
        Uri uri = new Uri(path);
        Console.WriteLine(label + uri.AbsoluteUri);
    }

    public static void Usage()
    {
        Info("Usage: ");
        Info("  <path>.tia                       compile the specified file");
        Info("  --test <path>                    test all files in the specified folder");
    }

}

enum TimerGroup
{
    TG_Compile   = 0,
    TG_Preload   = 1,
    TG_Lexer     = 2,
    TG_Parser    = 3,
    TG_Elab      = 4,
    TG_Print     = 5,
    TG_Analyser  = 6,
    TG_Layout    = 7,
    TG_ConstFold = 8,
    TG_IRGen     = 9,
    TG_LLVM      = 10,
}

#endregion