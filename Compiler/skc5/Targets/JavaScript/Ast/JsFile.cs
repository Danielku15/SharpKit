using System;
using System.Collections.Generic;
using SharpKit.Compiler;
using SharpKit.Compiler.Plugin;
using SharpKit.Targets.Ast;

namespace SharpKit.Targets.JavaScript.Ast
{
    public class JsFile : TargetFile
    {
        public List<JsUnit> Units { get; set; }

        public override void SaveAs(string filename)
        {
            SaveAs(filename, null, null);
        }

        internal void SaveAs(string filename, string format, ICompiler compiler)
        {
            var tmpFile = filename;
            using (var writer = JsWriter.Create(tmpFile, false))
            {
                var att = compiler.GetJsExportAttribute();
                if (format == null)
                    format = "JavaScript";
                writer.Format = format;
                try
                {
                    if (CompilerConfiguration.Current.EnableLogging && compiler != null)
                    {
                        writer.Visiting += node => compiler.Log.Debug(String.Format("JsWriter: Visit JsNode: {0}", filename));
                    }
                    foreach (var unit in Units)
                    {
                        if (unit.Tokens == null)
                            unit.Tokens = writer.GetTokens(unit);
                        else
                        {
                        }
                        var formatted = unit.TokensByFormat.TryGetValue(format);
                        if (formatted == null)
                        {
                            formatted = writer.FormatTokens(unit.Tokens);
                            unit.TokensByFormat[format] = formatted;
                        }
                        else
                        {
                        }
                        writer.WriteTokens(formatted);
                        writer.Flush();
                    }
                }
                catch (Exception e)
                {
                    if (compiler != null)
                        compiler.Log.Log(new CompilerLogItem { Type = CompilerLogItemType.Error, ProjectRelativeFilename = tmpFile, Text = e.Message });
                    throw e;
                }
            }
        }
    }

    public partial class JsExternalFileUnit : JsUnit
    {
        public string Filename { get; set; }
    }
}
