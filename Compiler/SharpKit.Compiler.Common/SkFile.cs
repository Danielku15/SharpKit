using System;
using SharpKit.Compiler.Plugin;
using SharpKit.Compiler.Targets.Ast;

namespace SharpKit.Compiler
{
    public abstract class SkFile
    {
        public ICompiler Compiler { get; set; }
        public string TempFilename { get; set; }
        public TargetFile TargetFile { get; set; }
        public string Format { get; set; }

        public abstract void Save();
    }

    public abstract class SkFile<TTargetFile> : SkFile
        where TTargetFile : TargetFile
    {
        public new TTargetFile TargetFile
        {
            get { return (TTargetFile)base.TargetFile; }
            set { base.TargetFile = value; }
        }

        public override string ToString()
        {
            if (TargetFile != null && TargetFile.Filename.IsNotNullOrEmpty())
                return TargetFile.Filename;
            return base.ToString();
        }
    }
}
