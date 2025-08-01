namespace TheiaLang;

public enum SymbolKind
{
    Function,
    Type,
    Variable,
}

public class SymbolInfo(string name,
                        TypeInfo type,
                        SymbolKind symbolKind,
                        List<TypeNamePair>? parameters)
{
    public string Name { get; init; } = name;
    public TypeInfo Type { get; set; } = type;
    public SymbolKind Kind { get; init; } = symbolKind;
    public List<TypeNamePair>? Parameters { get; init; } = parameters;
}

public class TypeInfo(string type,
                      TypeInfo? pointee = null,
                      TypeInfo? elementType = null,
                      List<int>? arrayLengths = null,
                      List<string>? fieldNames = null,
                      List<string>? fieldTypes = null)
{
    public string TypeName { get; init; } = type;

    public TypeInfo? Pointee { get; init; } = pointee;

    public TypeInfo? ElementType { get; set; } = elementType;
    public List<int>? ArrayLengths { get; set; } = arrayLengths;    // non-null for static arrays

    public List<string>? FieldNames { get; set; } = fieldNames;
    public List<string>? FieldTypes { get; set; } = fieldTypes;
}

public class Scope : INode
{
    public string Name;
    public string FullName =>
        Parent == null || string.IsNullOrEmpty(Parent.FullName)
            ? Name
            : $"{Parent.FullName}.{Name}";
    public INode? DeclaringNode;
    public Scope? Parent { get; }
    public Dictionary<string, Scope> Children { get; } = new Dictionary<string, Scope>();
    public Dictionary<string, SymbolInfo> Symbols { get; } = new Dictionary<string, SymbolInfo>();

    public Scope(string name, INode? declaringNode = null, Scope? parent = null)
    {
        Name = name;
        DeclaringNode = declaringNode;
        Parent = parent;
        if (parent == null)
            return;

        if (!parent.Children.ContainsKey(name))
            parent.Children.Add(name, this);
        else
            Log.Error(7, $"Identifier '{name}' already declared in the scope '{Parent!.FullName}'");
    }

    #region Helpers

    public void Declare(string name, SymbolInfo symbolInfo)
    {
        if (Symbols.ContainsKey(name))
            Log.Error(6, $"Identifier '{name}' already declared in the scope '{FullName}'");
        Symbols[name] = symbolInfo;
    }

    public bool TryLookup(string name, out SymbolInfo? symbolInfo, out Scope? symbolScope)
    {
        if (Symbols.TryGetValue(name, out symbolInfo))
        {
            symbolScope = this;
            return true;
        }
        else
        {
            if (Parent == null)
            {
                symbolInfo = null;
                symbolScope = null;
                return false;
            }
            else
                return Parent.TryLookup(name, out symbolInfo, out symbolScope);
        }
    }

    public bool GetParentOf(Scope scope, out Scope? parent)
    {
        if (Children.ContainsValue(scope))
        {
            parent = this;
            return true;
        }
        return Parent!.GetParentOf(scope, out parent);
    }

    #endregion
}