namespace TheiaLang;
using static BuiltinType;
using static TypeKind;

public enum BuiltinType
{
    VOID = 1,


    BOOL = 10,

    INT  = 20,
    S8   = 21,
    S16  = 22,
    S32  = 23,
    S64  = 24,
    S128 = 25,
    S256 = 26,

    U8   = 41,
    U16  = 42,
    U32  = 43,
    U64  = 44,
    U128 = 45,
    U256 = 46,

    FLOAT = 100,
    F16   = 101,
    F32   = 102,
    F64   = 103,
    F128  = 104,

    PTR = 200,
}

public static class Builtins
{
    public static readonly BuiltinType[] AllTypes = [
        VOID, 
        BOOL, 
        INT, 
            S8, S16, S32, S64, S128, S256, 
            U8, U16, U32, U64, U128, U256, 
        FLOAT,
            F16, F32, F64, F128, 
        PTR];

    public static int BuiltinTypeIndex(BuiltinType? builtinType) 
        => builtinType switch
        {
            null => -1,
            PTR  => 14,

            BOOL => 0,

            INT  => 1,
            S8   => 2,
            S16  => 3,
            S32  => 4,
            S64  => 5,
            S128 => 6,
            S256 => 7,

            FLOAT => 8,
            F16   => 9,
            F32   => 10,
            F64   => 11,
            F128  => 12,

            VOID  => 13,

            _    => throw new NotImplementedException(builtinType.ToString()),
        };

    public static bool IsInt(BuiltinType? builtinType)
        => builtinType switch
        {
            INT  => true,

            S8   => true,
            S16  => true,
            S32  => true,
            S64  => true,
            S128 => true,
            S256 => true,
            
            _ => false,
        };

    public static bool IsSignedInt(BuiltinType? builtinType)
        => builtinType switch
        {
            S8   => true,
            S16  => true,
            S32  => true,
            S64  => true,
            S128 => true,
            S256 => true,
            
            _ => false,
        };

    public static bool IsFloat(BuiltinType? builtinType)
        => builtinType switch
        {
            FLOAT => true,
            F16   => true,
            F32   => true,
            F64   => true,
            F128  => true,
            
            _ => false,
        };

    public static bool IsIEE754Float(BuiltinType? builtinType)
        => builtinType switch
        {
            F16   => true,
            F32   => true,
            F64   => true,
            F128  => true,
            
            _ => false,
        };

    public static TypeInfo GetTypeInfo(BuiltinType builtin)
        => builtin switch
        {
            VOID => new TypeInfo("void", Void, VOID, 0),

            BOOL => new TypeInfo("bool", Scalar, BOOL, 1),

            S8   => new TypeInfo("s8",   Scalar, S8,   1),
            S16  => new TypeInfo("s16",  Scalar, S16,  2),
            S32  => new TypeInfo("s32",  Scalar, S32,  4),
            S64  => new TypeInfo("s64",  Scalar, S64,  8),
            S128 => new TypeInfo("s128", Scalar, S128, 16),
            S256 => new TypeInfo("s256", Scalar, S256, 32),

            U8   => new TypeInfo("u8",   Scalar, U8,   1),
            U16  => new TypeInfo("u16",  Scalar, U16,  2),
            U32  => new TypeInfo("u32",  Scalar, U32,  4),
            U64  => new TypeInfo("u64",  Scalar, U64,  8),
            U128 => new TypeInfo("u128", Scalar, U128, 16),
            U256 => new TypeInfo("u256", Scalar, U256, 32),

            F16  => new TypeInfo("f16",  Scalar, F16,  2),
            F32  => new TypeInfo("f32",  Scalar, F32,  4),
            F64  => new TypeInfo("f64",  Scalar, F64,  8),
            F128 => new TypeInfo("f128", Scalar, F128, 16),

            PTR  => new TypeInfo("@void", Pointer, PTR, 8, pointee: GetTypeInfo(VOID)),

            _ => throw new NotImplementedException(),
        };
}