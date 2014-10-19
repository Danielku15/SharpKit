using SharpKit.Compiler.Plugin;

namespace SharpKit.Compiler.Java
{
    class JavaCompilerService : CompilerService
    {
        protected override ICompiler CreateCompiler()
        {
            return new JavaCompiler();
        }
    }
}
