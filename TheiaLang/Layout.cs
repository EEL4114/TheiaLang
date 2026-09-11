namespace TheiaLang;

using static TypeKind;
using static BuiltinType;

public enum CompilationTarget
{
    x86_64_windows = 0,
}

public class TypeLayout(uint size, uint alignment)
{
    public uint Size { get; } = size;
    public uint Alignment { get; } = alignment;
}

public sealed class StructLayout(
    uint size,
    uint alignment,
    IReadOnlyList<uint> fieldOffsets)
    : TypeLayout(size, alignment)
{
    public IReadOnlyList<uint> FieldOffsets { get; } = fieldOffsets;
}

public class Layout(CompilationTarget target)
{
    Dictionary<string, TypeLayout> Layouts = [];

    public CompilationTarget Target = target;

    public void GenerateLayout(ProgramNode program)
    {
        foreach(INode node in program.Nodes)
        {
            if (node is StructDeclaration sd)
            {
                if(Layouts.TryAdd(sd.ResolvedType.TypeName, null!))
                    Layouts[sd.ResolvedType.TypeName] = GenerateLayout(sd.ResolvedType);
                
            }
            else if(node is UnionDeclaration ud)
            {
                if(Layouts.TryAdd(ud.ResolvedType.TypeName, null!))
                    Layouts[ud.ResolvedType.TypeName] = GenerateLayout(ud.ResolvedType);
            }
        }
    }

    public TypeLayout GetLayout(TypeInfo type)
    {
        switch(type.TypeKind)
        {
            case Unresolved: 
                throw new InvalidOperationException("Unresolved has no ABI layout");
            case Void:
                throw new InvalidOperationException("void has no ABI layout");
            case Pointer or Scalar: 
                return BuiltinLayouts[Target][(BuiltinType)type.BuiltinType!];
            case Array:
                TypeLayout elementLayout = GetLayout(type.ElementType!);
                return new TypeLayout(elementLayout.Size * (uint)type.ArrayLength!, elementLayout.Alignment);
            case Struct or Union:
                return Layouts[type.TypeName];
            default:
                throw new NotImplementedException($"No layout implemented for {type.TypeKind}");
        }
    }

    TypeLayout GenerateLayout(TypeInfo type)
    {
        switch(type.TypeKind)
        {
            case Unresolved: 
                throw new InvalidOperationException("Unresolved has no ABI layout");
            case Void:
                throw new InvalidOperationException("void has no ABI layout");
            case Pointer or Scalar: 
                return BuiltinLayouts[Target][(BuiltinType)type.BuiltinType!];
            case Array:
                TypeLayout elementLayout = GenerateLayout(type.ElementType!);
                return new TypeLayout(elementLayout.Size * (uint)type.ArrayLength!, elementLayout.Alignment);
            case Struct:
                uint maxFieldAlignment = 1;
                uint offset = 0;

                List<uint> fieldOffsets = [];

                foreach(TypeInfo fieldInfo in type.FieldTypes!)
                {
                    TypeLayout fieldLayout = GenerateLayout(fieldInfo);
                    maxFieldAlignment = Math.Max(maxFieldAlignment, fieldLayout.Alignment);
                    offset = AlignUp(offset, fieldLayout.Alignment);
                    fieldOffsets.Add(offset);
                    offset += fieldLayout.Size;
                }

                return new StructLayout(AlignUp(offset, maxFieldAlignment), maxFieldAlignment, fieldOffsets);
            case Union:
                uint maxVariantAlignment = 0;
                uint maxVariantSize = 0;

                foreach(TypeInfo variantInfo in type.FieldTypes!)
                {
                    TypeLayout variantLayout = GenerateLayout(variantInfo);
                    maxVariantAlignment = Math.Max(maxVariantAlignment, variantLayout.Alignment);
                    maxVariantSize = Math.Max(maxVariantSize, variantLayout.Size);
                }

                return new TypeLayout(AlignUp(maxVariantSize, maxVariantAlignment), maxVariantAlignment);
            default:
                throw new NotImplementedException($"No layout implemented for {type.TypeKind}");
        }
    }

    #region Helpers

    static uint AlignUp(uint value, uint alignment)
        => (value + alignment - 1) & ~(alignment - 1);

    #endregion


    static readonly Dictionary<CompilationTarget, Dictionary<BuiltinType, TypeLayout>> BuiltinLayouts
        = new Dictionary<CompilationTarget, Dictionary<BuiltinType, TypeLayout>>
    {   {CompilationTarget.x86_64_windows, new Dictionary<BuiltinType, TypeLayout> {
                {VOIDPTR, new TypeLayout(8, 8)},

                {BOOL, new TypeLayout(1, 1)},

                {S8,   new TypeLayout(1, 1)},
                {S16,  new TypeLayout(2, 2)},
                {S32,  new TypeLayout(4, 4)},
                {S64,  new TypeLayout(8, 8)},
                {S128, new TypeLayout(16, 16)},
                {S256, new TypeLayout(32, 16)},

                {U8,   new TypeLayout(1, 1)},
                {U16,  new TypeLayout(2, 2)},
                {U32,  new TypeLayout(4, 4)},
                {U64,  new TypeLayout(8, 8)},
                {U128, new TypeLayout(16, 16)},
                {U256, new TypeLayout(32, 16)},

                {F16,  new TypeLayout(2, 2)},
                {F32,  new TypeLayout(4, 4)},
                {F64,  new TypeLayout(8, 8)},
                {F128, new TypeLayout(16, 16)},
            }
        }
    };
}