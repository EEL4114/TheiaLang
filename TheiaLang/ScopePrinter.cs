using TheiaLang;

static class ScopePrinter
{
    public static void Print(string ProgramName, Scope scope, TextWriter w, bool includeTimestamp = true)
    {
        w.WriteLine($"Program: {ProgramName}");
        if (includeTimestamp)
            w.WriteLine($"Generated at {DateTime.Now}");

        w.WriteLine();
        PrintScope(scope, w, 0);
    }

    static void PrintScope(Scope scope, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}Scope: {scope.Name}");
        if (scope.Symbols != null)
        {
            w.WriteLine($"{Indent(indent + 1)}Symbols:");
            foreach (KeyValuePair<string, SymbolInfo> symbol in scope.Symbols)
                w.WriteLine($"{Indent(indent + 2)}{symbol.Value.Type.TypeName} {symbol.Key}");
        }

        if (scope.Children != null)
        {
            w.WriteLine($"{Indent(indent + 1)}Children:");
            foreach (KeyValuePair<string, Scope> child in scope.Children)
                PrintScope(child.Value, w, indent + 2);
        }
    }

    static string Indent(int n) => new string('\t', n);
}