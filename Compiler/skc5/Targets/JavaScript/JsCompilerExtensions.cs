using SharpKit.Compiler;
using SharpKit.Compiler.Plugin;

namespace SharpKit.Targets.JavaScript
{
    static class JsCompilerExtensions
    {
        public static JsExportAttribute GetJsExportAttribute(this ICompiler compiler)
        {
            if (!compiler.TargetData.ContainsKey("JsExportAttribute"))
            {
                compiler.TargetData["JsExportAttribute"] = Sk.GetJsExportAttribute(compiler.Project.Compilation);
            }
            return (JsExportAttribute) compiler.TargetData["JsExportAttribute"];
        }
    }
}
