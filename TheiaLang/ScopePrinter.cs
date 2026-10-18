using TheiaLang;

static class ScopePrinter
{
    public static void Print(string ProgramName, Scope scope, List<TypeInfo> types, TextWriter w, bool includeTimestamp = true)
    {
        w.WriteLine($"Program: {ProgramName}");
        if (includeTimestamp)
            w.WriteLine($"Generated at {DateTime.Now}");

        w.WriteLine();
        PrintScope(scope, types, w, 0);
    }

    static void PrintScope(Scope scope, List<TypeInfo> types, TextWriter w, int indent)
    {
        w.WriteLine($"{Indent(indent)}Scope: {scope.Name}");
        if (scope.Symbols != null)
        {
            w.WriteLine($"{Indent(indent + 1)}Symbols:");
            foreach (KeyValuePair<string, SymbolInfo> symbol in scope.Symbols)
                w.WriteLine($"{Indent(indent + 2)}{types[symbol.Value.TypeId].TypeName} {symbol.Key} {symbol.Value.TypeId}");
        }

        if (scope.Children != null)
        {
            w.WriteLine($"{Indent(indent + 1)}Children:");
            foreach (KeyValuePair<string, Scope> child in scope.Children)
                PrintScope(child.Value, types, w, indent + 2);
        }
    }

    static string Indent(int n) => new string(' ', 2 * n);
}