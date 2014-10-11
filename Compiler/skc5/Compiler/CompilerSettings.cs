using System.Collections.Generic;
using System.IO;
using Corex.IO.Tools;

namespace SharpKit.Compiler
{
    public class CompilerSettings
    {
        static readonly ToolArgsInfo<CompilerSettings> Info = new ToolArgsInfo<CompilerSettings> { Error = System.Console.WriteLine };

        private string _assemblyName;

        [ToolArgCommand]
        public List<string> Files { get; private set; }

        public string Service { get; set; }

        [ToolArgSwitch("?")]
        public bool Help { get; set; }

        /// <summary>
        /// designates the current directory that all paths are relative to
        /// </summary>
        [ToolArgSwitch("dir")]
        public string CurrentDirectory { get; set; }

        [ToolArgSwitch("target")]
        public string Target { get; set; }

        [ToolArgSwitch("out")]
        public string Output { get; set; }

        [ToolArgSwitch("reference")]
        public List<string> References { get; private set; }

        [ToolArgSwitch("plugin")]
        public List<string> Plugins { get; private set; }

        [ToolArgSwitch("contentfile")]
        public List<string> ContentFiles { get; private set; }

        [ToolArgSwitch("resource")]
        public List<string> ResourceFiles { get; private set; }

        [ToolArgSwitch("nonefile")]
        public List<string> NoneFiles { get; private set; }

        [ToolArgSwitch("why")]
        public bool Why { get; set; }

        [ToolArgSwitch("rebuild")]
        public bool? Rebuild { get; set; }

        public bool? Enabled { get; set; }
        public bool? ExportToCSharp { get; set; }
        public bool? DebuggerBreak { get; set; }
        public bool? noconfig { get; set; }
        public bool? UseLineDirectives { get; set; }

        public string errorreport { get; set; }

        public int warn { get; set; }

        public string nowarn { get; set; }

        public string define { get; set; }

        [ToolArgSwitch("debug")]
        public string debugLevel { get; set; }

        public bool? debug { get; set; }

        public bool? optimize { get; set; }

        [ToolArgSwitch("filealign")]
        public int filealign { get; set; }

        public string AssemblyName
        {
            get { return _assemblyName ?? (_assemblyName = Path.GetFileNameWithoutExtension(Output)); }
        }

        public string ManifestFile { get; set; }

        public string CodeAnalysisFile { get; set; }

        public string SecurityAnalysisFile { get; set; }

        public string OutputGeneratedJsFile { get; set; }

        public string OutputGeneratedFile { get; set; }

        public string OutputGeneratedDir { get; set; }

        public bool CheckForNewVersion { get; set; }

        /// <summary>
        /// /addbuildtarget:"pathToCsprojFile"
        /// /addbuildtarget:"pathToCsprojFile";nuget
        /// </summary>
        [ToolArgSwitch]
        public string AddBuildTarget { get; set; }

        public string TargetFrameworkVersion { get; set; }

        [ToolArgSwitch("ngen")]
        public bool CreateNativeImage { get; set; }

        public bool LastArgs { get; set; }

        public CompilerSettings()
        {
            Files = new List<string>();
            References = new List<string>();
            ContentFiles = new List<string>();
            NoneFiles = new List<string>();
            ResourceFiles = new List<string>();
        }

        public static CompilerSettings Parse(string[] args)
        {
            return Info.Parse(args);
        }
        
        public static void GenerateHelp(TextWriter writer)
        {
            Info.HelpGenerator.Generate(writer);
        }
    }
}
