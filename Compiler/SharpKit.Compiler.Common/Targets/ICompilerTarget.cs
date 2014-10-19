using System;
using System.Collections.Generic;
using System.Diagnostics;
using SharpKit.Compiler.Plugin;
using SharpKit.Compiler.Targets.Ast;

namespace SharpKit.Compiler.Targets
{
    public interface ICompilerTarget
    {
        CompilerTarget Target { get; }
        ICompiler Compiler { get; set; }
        string OutputSuffix { get; }

        IEnumerable<string> ManifestFiles { get; }

        ICsExternalMetadata BuildExternalMetadata();
        ITypeConverter BuildTypeConverter();
        void MergeTargetFiles();
        void InjectTargetCode();
        void OptimizeTargetFiles();
        void SaveTargetFiles();
        void EmbedResources();
        SkFile CreateSkFile(TargetFile arg);
    }

    public abstract class CompilerTargetBase : ICompilerTarget
    {
        public abstract CompilerTarget Target { get; }
        public ICompiler Compiler { get; set; }

        public virtual string OutputSuffix
        {
            get { return string.Empty; }
        }

        public abstract IEnumerable<string> ManifestFiles { get; }

        public abstract ICsExternalMetadata BuildExternalMetadata();
        public abstract ITypeConverter BuildTypeConverter();
        public abstract void MergeTargetFiles();
        public abstract void InjectTargetCode();
        public abstract void OptimizeTargetFiles();
        public abstract void SaveTargetFiles();
        public abstract void EmbedResources();
        public abstract SkFile CreateSkFile(TargetFile file);

        [DebuggerStepThrough]
        protected void Time(Action action)
        {
            var stopwatch = new Stopwatch();
            Compiler.Log.WriteLine("{0:HH:mm:ss.fff}: {1}: Start: ", DateTime.Now, action.Method.Name);
            stopwatch.Start();
            action();
            stopwatch.Stop();
            Compiler.Log.WriteLine("{0:HH:mm:ss.fff}: {1}: End: {2}ms", DateTime.Now, action.Method.Name, stopwatch.ElapsedMilliseconds);
        }

        [DebuggerStepThrough]
        protected T Time2<T>(Func<T> action)
        {
            var stopwatch = new Stopwatch();
            Compiler.Log.WriteLine("{0:HH:mm:ss.fff}: {1}: Start: ", DateTime.Now, action.Method.Name);
            stopwatch.Start();
            var t = action();
            stopwatch.Stop();
            Compiler.Log.WriteLine("{0:HH:mm:ss.fff}: {1}: End: {2}ms", DateTime.Now, action.Method.Name, stopwatch.ElapsedMilliseconds);
            return t;
        }

    }
}
