using System;
using System.IO;
using SharpKit.Compiler.Targets.Java.Ast;
using SharpKit.Compiler.Targets.Utils;

namespace SharpKit.Compiler.Targets.Java
{
    class SkJFile : SkFile<JFile>
    {
        public override void Save()
        {
            var jFile = TargetFile;
            Compiler.Log.WriteLine("    {0}", jFile.Filename);
            if (TempFilename.IsNullOrEmpty())
                TempFilename = jFile.Filename + ".tmp";
            var dir = Path.GetDirectoryName(TempFilename);
            if (dir.IsNotNullOrEmpty() && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            jFile.SaveAs(TempFilename);
            FileUtils.CompareAndSaveFile(jFile.Filename, TempFilename);
        }
    }
}
