using SharpKit.Compiler.Targets;
using SharpKit.Compiler.Targets.Java;

namespace SharpKit.Compiler.Java
{
    class JavaCompiler : TargetBasedCompiler
    {
        protected override ICompilerTarget BuildTarget()
        {
            return new JavaTarget();
        }

        protected override CompilerService CreateCompilerService()
        {
            return new JavaCompilerService();
        }
    }
}
