using Mirrored.SharpKit.JavaScript;
using SharpKit.Compiler;

namespace SharpKit.Targets.JavaScript
{
    static class JsCompilerExtensions
    {
        public static JsExportAttribute GetJsExportAttribute(this ICompiler compiler)
        {
            if (compiler.TargetData["JsExportAttribute"] == null)
            {
                compiler.TargetData["JsExportAttribute"] = Sk.GetJsExportAttribute(compiler.Project.Compilation);
            }
            return (JsExportAttribute) compiler.TargetData["JsExportAttribute"];
        }
    }
}
