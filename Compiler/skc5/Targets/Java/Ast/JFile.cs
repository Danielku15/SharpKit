using System;
using System.Collections.Generic;
using SharpKit.Compiler;
using SharpKit.Targets.Ast;

namespace SharpKit.Java.Ast
{
    class JFile : TargetFile
    {
        public List<JCompilationUnit> Units { get; set; }
        public ICompiler Compiler { get; set; }

        public JFile()
        {
            Units = new List<JCompilationUnit>();
        }

        public override void SaveAs(string filename)
        {
            var tmpFile = filename;
            using (var writer = JWriter.Create(tmpFile, false))
            {
                try
                {
                    if (CompilerConfiguration.Current.EnableLogging)
                    {
                        writer.Visiting += node =>
                            {
                                Compiler.Log.Debug(String.Format("JWriter: Visit JNode: {0}: [{1}, {2}]", filename, writer.CurrentLine, writer.CurrentColumn));
                            };
                    }
                    writer.VisitEach(Units);
                }
                catch (Exception e)
                {
                    Compiler.Log.Log(new CompilerLogItem { Type = CompilerLogItemType.Error, ProjectRelativeFilename = tmpFile, Text = e.Message });
                    throw e;
                }
            }
        }
    }

    partial class JExternalFileUnit : JUnit
    {
        public string Filename { get; set; }
    }
}
