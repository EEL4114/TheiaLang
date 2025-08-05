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
                      uint size = 0,
                      TypeInfo? pointee = null,
                      TypeInfo? elementType = null,
                      List<uint>? arrayLengths = null,
                      List<string>? fieldNames = null,
                      List<string>? fieldTypes = null)
{
    public string TypeName { get; init; } = type;

    public TypeInfo? Pointee { get; init; } = pointee;

    public TypeInfo? ElementType { get; set; } = elementType;
    public List<uint>? ArrayLengths { get; set; } = arrayLengths;    // non-null for static arrays

    public List<string>? FieldNames { get; set; } = fieldNames;
    public List<string>? FieldTypes { get; set; } = fieldTypes;
    public uint Size { get; set; } = size;
    public override string ToString()
    {
        string s = $"{TypeName}: Size = {Size} B";

        if (Pointee != null)
            s += $", Pointee = {Pointee}";
        if (ElementType != null)
            s += $", ElementType = {ElementType}";
        if (ArrayLengths != null && ArrayLengths.Count != 0)
        {
            s += ", ArrayLengths = {";
            s += ArrayLengths[0];
            for (int i = 1; i < ArrayLengths.Count; i++)
                s += ", " + ArrayLengths[i];
            s += "}";
        }
        // @Robust
        if (FieldNames != null && FieldNames.Count != 0)
        {
            s += ", Fields = {";
            s += FieldTypes![0] + " " + FieldNames[0];
            for (int i = 1; i < FieldNames.Count; i++)
                s += ", " + FieldTypes![i] + " " + FieldNames[i];
            s += "}";
        }
        return s;
    }
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
    public Dictionary<string, Scope> Children { get; } = [];
    public Dictionary<string, SymbolInfo> Symbols { get; } = [];

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

    // TODO it may be useful to have a version of this function that always returns or errors
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