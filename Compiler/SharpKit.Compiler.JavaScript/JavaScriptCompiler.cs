using SharpKit.Compiler.Targets;

namespace SharpKit.Compiler.JavaScript
{
    class JavaScriptCompiler : TargetBasedCompiler
    {
        protected override ICompilerTarget BuildTarget()
        {
            return new JavaScriptTarget();
        }

        protected override CompilerService CreateCompilerService()
        {
            return new JavaScriptCompilerService();
        }
    }
}
