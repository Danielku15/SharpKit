﻿using System;
using System.IO;
using SharpKit.Compiler.JavaScript.Ast;
using SharpKit.Compiler.JavaScript.SourceMapping;
using SharpKit.Compiler.JavaScript.Utils;
using SharpKit.Compiler.Targets.Utils;

namespace SharpKit.Compiler.JavaScript
{
    class SkJsFile : SkFile<JsFile>
    {
        public bool GenerateSourceMap { get; set; }
        public bool Minify { get; set; }
        public string TempFilename { get; set; }

        public override void Save()
        {
            var jsFile = TargetFile;
            Compiler.Log.WriteLine("    {0}", jsFile.Filename);
            var ext = Path.GetExtension(jsFile.Filename).ToLower();
            if (CssCompressorExtensions.IsNullOrEmpty(TempFilename))
                TempFilename = jsFile.Filename + ".tmp";
            var dir = Path.GetDirectoryName(TempFilename);
            if (dir.IsNotNullOrEmpty() && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            jsFile.SaveAs(TempFilename, Minify ? "Minified" : Format, Compiler);
            if (Minify)
            {
                if (ext == ".js")
                {
                    //FileUtils.JsMinify(TempFilename);
                }
                else if (ext == ".css")
                    FileUtils.CssMinify(TempFilename);
                else
                    Compiler.Log.Warn("Cannot minify file:" + jsFile.Filename + " unknown extension");
            }
            if (GenerateSourceMap)
            {
                var smg = new SkSourceMappingGenerator { Compiler = Compiler };
                smg.TryGenerateAndAddMappingDirective(this);
            }
            FileUtils.CompareAndSaveFile(jsFile.Filename, TempFilename);    
        }
    }
}
