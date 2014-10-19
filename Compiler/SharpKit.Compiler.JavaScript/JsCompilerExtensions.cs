using SharpKit.Compiler.Plugin;

namespace SharpKit.Compiler.JavaScript
{
    static class JsCompilerExtensions
    {
        public static JsExportAttribute GetJsExportAttribute(this ICompiler compiler)
        {
            if (!compiler.TargetData.ContainsKey("JsExportAttribute"))
            {
                compiler.TargetData["JsExportAttribute"] = Utils.Sk.GetJsExportAttribute(compiler.Project.Compilation);
            }
            return (JsExportAttribute) compiler.TargetData["JsExportAttribute"];
        }
    }
}
