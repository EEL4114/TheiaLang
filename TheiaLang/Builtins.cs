namespace TheiaLang;
using static BuiltinType;

public enum BuiltinType
{
    VOID = 1,


    BOOL = 10,

    S8   = 20,
    S16  = 21,
    S32  = 22,
    S64  = 23,
    S128 = 24,
    S256 = 25,

    U8   = 30,
    U16  = 31,
    U32  = 32,
    U64  = 33,
    U128 = 34,
    U256 = 35,

    F16  = 100,
    F32  = 101,
    F64  = 102,
    F128 = 103,

    PTR = 200,
}

public static class Builtins
{
    public static readonly BuiltinType[] AllTypes = [VOID, BOOL, S8, S16, S32, S64, S128, S256, U8, U16, U32, U64, U128, U256, F16, F32, F64, F128, PTR];
}