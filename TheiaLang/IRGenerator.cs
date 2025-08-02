using LLVMSharp.Interop;

namespace TheiaLang;

public static class IRGenerator
{
    static LLVMContextRef context;
    static LLVMModuleRef module;
    static LLVMBuilderRef builder;

    public static void GenerateIR(ProgramNode program, Scope globalScope)
    {
        context = LLVMContextRef.Create();
        module = context.CreateModuleWithName("TheiaModule");
        builder = context.CreateBuilder();
    }
}