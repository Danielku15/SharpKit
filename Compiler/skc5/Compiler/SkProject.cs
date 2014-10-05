using System;
using ICSharpCode.NRefactory.TypeSystem;
using System.Collections.Concurrent;
using ICSharpCode.NRefactory.Extensions;

namespace SharpKit.Compiler
{
    class SkProject : NProject
    {
        public ConcurrentDictionary<IAttribute, object> AttributeCache = new ConcurrentDictionary<IAttribute, object>();

        private static NAssemblyCache Cache = new NAssemblyCache
        {
            //IdleTimeToClear = TimeSpan.FromMinutes(1),
        };

        public ICompiler Compiler { get; set; }
        public CompilerLogger Log { get; set; }

        public SkProject()
        {
            Parallel = CollectionExtensions.Parallel;
            AssemblyCache = Cache;
        }

        protected override void ParseCsFiles()
        {
            base.ParseCsFiles();
            if (!CSharpParser.HasErrors)
                return;
            foreach (var error in CSharpParser.ErrorsAndWarnings)
            {
                var item = new CompilerLogItem
                {
                    ProjectRelativeFilename = error.Region.FileName,
                    Line = error.Region.BeginLine,
                    Column = error.Region.BeginColumn,
                    Text = error.Message,
                    Type = CompilerLogItemType.Error,
                };
                if (error.ErrorType == ErrorType.Warning)
                    item.Type = CompilerLogItemType.Warning;
                Log.Log(item);

            }
        }

        protected override void WriteLine(object obj)
        {
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}", DateTime.Now, obj);
        }

        protected override void FormatLine(string format, params object[] args)
        {
            WriteLine(String.Format(format, args));
        }
    }
}
