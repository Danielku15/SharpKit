using SharpKit.Compiler.Plugin;

namespace SharpKit.Compiler.JavaScript
{
    class JavaScriptCompilerService : CompilerService
    {
        protected override ICompiler CreateCompiler()
        {
            return new JavaScriptCompiler();
        }
    }
}
