namespace TheiaLang;

public enum SymbolKind
{
    Function,
    Type,
    Variable,
    Namespace,
}

    public class SymbolInfo(string name,
                            TypeInfo type,
                            SymbolKind symbolKind,
                            List<TypeNamePair>? parameters)
    {
        public string Name { get; init; } = name;
        public TypeInfo Type = type;
        public SymbolKind Kind { get; init; } = symbolKind;
        public List<TypeNamePair>? Parameters { get; init; } = parameters;

    public override string ToString()
    {
        string s = $"{Kind} {Name}: {Type}";
        if (Parameters != null && Parameters.Count > 0)
        {
            s += $" (\n    {Parameters[0].ResolvedType.TypeName} {Parameters[0].Identifier}";
            for (int i = 1; i < Parameters!.Count; i++)
                s += $",\n    {Parameters[i].ResolvedType.TypeName} {Parameters[i].Identifier}";
            s += ")";
        }
        return s;
    }
}

public enum TypeKind
{
    Unresolved = -1,
    Void    = 0,
    Scalar  = 1,
    Array   = 2,
    Struct  = 3,
    Union   = 4,
    Pointer = 5,
}

public class TypeInfo(string type,
                      TypeKind typeKind,
                      uint size = 0,
                      TypeInfo? pointee = null,
                      TypeInfo? elementType = null,
                      uint? arrayLength = null,
                      List<string>? fieldNames = null,
                      List<TypeInfo>? fieldTypes = null)
{
    public string TypeName { get; init; } = type;
    
    public TypeKind TypeKind {get; set; } = typeKind; 

    public TypeInfo? Pointee { get; set; } = pointee;

    public TypeInfo? ElementType { get; set; } = elementType;
    public uint? ArrayLength { get; set; } = arrayLength;    // non-null for static arrays

    public List<string>? FieldNames { get; set; } = fieldNames;
    public List<TypeInfo>? FieldTypes { get; set; } = fieldTypes;
    public uint Size { get; set; } = size;
    public override string ToString()
    {
        string s = $"{TypeName}: Size = {Size} B";

        if (Pointee != null)
            s += $", Pointee = {Pointee}";
        if (ElementType != null)
            s += $", ElementType = {ElementType}";
        if (ArrayLength != null)
            s += $", Length = {ArrayLength}";
        // @Robust
        if (FieldNames != null && FieldNames.Count != 0)
        {
            s += ",\nFields = {";
            s += $"\n    {FieldTypes![0]} {FieldNames[0]}";
            for (int i = 1; i < FieldNames.Count; i++)
                s += $",\n    {FieldTypes![i]} {FieldNames[i]}";
            s += "\n}";
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
    public SourePosition Pos => default;

    public Scope(string name, INode? declaringNode = null, Scope? parent = null)
    {
        Name = name;
        DeclaringNode = declaringNode;
        Parent = parent;
        if (Parent == null)
            return;

        if (!Parent.Children.ContainsKey(name))
            Parent.Children.Add(name, this);   
        else
            Log.Error(7, $"Identifier '{name}' is already declared in the scope '{Parent!.FullName}'{TryPos(declaringNode)}");

        //Log.Info($"Declare Scope '{name}' in parent scope '{Parent.FullName}'.\n\tAdded to parent children: {Parent.Children.ContainsKey(name)}");
    }

    #region Helpers

    public void Declare(SymbolInfo symbolInfo)
    {
        if (Symbols.ContainsKey(symbolInfo.Name))
            Log.Error(6, $"Identifier '{symbolInfo.Name}' is already declared in the scope '{FullName}'");
        Symbols[symbolInfo.Name] = symbolInfo;
    }

    public Scope Exit()
    {
        if (Parent == null)
            throw new Exception("Attempted to exit global scope");

        return Parent;
    }

    // TODO it may be useful to have a version of this function that always returns or errors
    // can we remove symbolScope as return value here?
    public bool TryLookup(string identifier, out SymbolInfo? symbolInfo, out Scope? symbolScope)
    {
        if (Symbols.TryGetValue(identifier, out symbolInfo))
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
                return Parent.TryLookup(identifier, out symbolInfo, out symbolScope);
        }
    }

    public bool TryLookupLocal(string identifier, out SymbolInfo? symbolInfo, out Scope? symbolScope)
    {
        if (Symbols.TryGetValue(identifier, out symbolInfo))
        {
            symbolScope = this;
            return true;
        }
        else
        {
            symbolScope = null;
            return false;
        }
    }

    static string TryPos(INode? node) => node == null || node.Pos == SourePosition.None ? " | ()" : $" | ({node.Pos.Row} : {node.Pos.Column})";


    public bool TryFindChild(string name, out Scope? childScope)
    {
        if (Children.TryGetValue(name, out Scope? scope))
        {
            childScope = scope;
            return true;
        }
        else
        {
            if (Parent == null)
            {
                childScope = null;
                return false;
            }
            else
                return Parent.TryFindChild(name, out childScope);
        }
    }

    public bool IsScopeDefined(Scope scope)
    {
        if (scope.Name == Name || Children.ContainsKey(scope.Name))
            return true;
        else
        {
            if (Parent == null)
                return false;
            else
                return Parent.IsScopeDefined(scope);
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

    public override string ToString() => string.IsNullOrEmpty(FullName) ? "Global" 
                                                                        : FullName;
    #endregion
}