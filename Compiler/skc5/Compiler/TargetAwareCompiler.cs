using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Corex.Helpers;
using Corex.IO.Tools;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.Ast;
using SharpKit.Compiler.CsToJs;
using SharpKit.Compiler.JavaScript;
using SharpKit.Targets;
using SharpKit.Targets.Java;
using SharpKit.Targets.JavaScript;
using SharpKit.Utils.Http;

namespace SharpKit.Compiler
{
    public enum CompilerTarget
    {
        JavaScript,
        Java
    }
    class TargetAwareCompiler : ICompiler
    {
        public string[] CommandLineArguments { get; set; }
        public CompilerSettings Settings { get; set; }
        public string SkcVersion { get; set; }

        public SkProject Project { get; set; }
        public List<SkFile> SkFiles { get; private set; }

        public CompilerLogger Log { get; set; }
        public ICompilation Compilation
        {
            get { return Project.Compilation; }
        }

        public ICompilerTarget Target { get; set; }
        public List<string> Defines { get; set; }

        public ICustomAttributeProvider CustomAttributeProvider { get; set; }
        public ICsExternalMetadata ExternalMetadata { get; set; }
        public ITypeConverter TypeConverter { get; set; }
        public PathMerger PathMerger { get; set; }

        public Dictionary<string, object> TargetData { get; private set; }

        public TargetAwareCompiler()
        {
            CompilerConfiguration.LoadCurrent();
            TargetData = new Dictionary<string, object>();
        }

        public void Init()
        {
            if (Log == null)
            {
                Log = new CompilerLogger();
                Log.Init();
            }

        }

        public int Run()
        {
            TargetData.Clear();
            var x = InternalRun();
            OnBeforeExit();
            return x;
        }

        private int InternalRun()
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                SkcVersion = typeof(TargetAwareCompiler).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;

                #region Argument Parsing

                Time(SaveLastInputArgs);
                WriteArgs();
                Time(ParseArgs);
                if (Settings.LastArgs)
                {
                    var tokenizer = new ToolArgsTokenizer();
                    var args = tokenizer.Tokenize(_lastArgs);
                    CommandLineArguments = args;
                    Time(SaveLastInputArgs);
                    WriteArgs();
                    Time(ParseArgs);
                }
                if (Settings.CheckForNewVersion)
                {
                    Time(CheckForNewVersion);
                    if (Settings.Files.IsNullOrEmpty())
                        return 0;
                }

                #endregion

                if (Help())
                    return 0;

                #region Service

                if (Settings.Service != null)
                {
                    var action = Settings.Service.ToLower();
                    switch (action)
                    {
                        case "start":
                            StartWindowsService();
                            break;
                        case "stop":
                            StopWindowsService();
                            break;
                        case "restart":
                            StopWindowsService();
                            StartWindowsService();
                            break;
                        case "install":
                            InstallWindowsService();
                            break;
                        case "uninstall":
                            UninstallWindowsService();
                            break;
                        case "reinstall":
                            Log.WriteLine("Reinstalling Service");
                            UninstallWindowsService();
                            InstallWindowsService();
                            break;
                        case "console":
                            Log.WriteLine("Starting Console Service");
                            RunInServiceConsoleMode();
                            break;
                        case "windows":
                            Log.WriteLine("Starting Windows Service");
                            RunInWindowsServiceMode();
                            break;
                    }
                    return 0;
                }

                #endregion

                if (!Time2(CalculateMissingArgs))
                    return -1;

                if (Time2(CheckManifest))
                    return 0;

                // Note: Native image generation removed since our target language is not .net

                Time(LoadPlugins);

                #region Compilation

                // 1. Compile C# 
                Time(ParseCs);

                // 2. Load metadata applied to types
                Time(ApplyExternalMetadata);

                // 3. Convert C# to Target Model
                Time(ConvertCsToTarget);

                // 4. Merge files
                Time(MergeTargetFiles);

                // 5. Inject code
                Time(InjectTargetCode);

                // 6. Optimize Target Files
                Time(OptimizeTargetFiles);

                // 7. Save Files
                Time(SaveTargetFiles);

                // 8. Resource Embedding
                Time(EmbedResources);

                if (Log.Items.Any(t => t.Type == CompilerLogItemType.Error))
                    return -1;

                Time(SaveNewManifest);

                #endregion

                return 0;
            }
            catch (Exception e)
            {
                Log.Log(e);
                Log.Console.WriteLine(e);
                return -1;
            }
        }

        #region Argument Parsing

        private string _lastArgs;
        private void SaveLastInputArgs()
        {
            var file = Process.GetCurrentProcess().MainModule.FileName.ToFileInfo().Directory.GetFile("prms.txt");
            if (file.Exists)
                _lastArgs = file.ReadAllText();
            var s = ArgsToString();
            try
            {
                file.WriteAllText(s);
            }
            catch
            {
            }
        }

        private string ArgsToString()
        {
            var sb = new StringBuilder();
            CommandLineArguments.ForEachJoin(arg =>
            {
                if (arg.StartsWith("@"))
                    sb.Append(File.ReadAllText(arg.Substring(1)));
                else
                    sb.Append(arg);
            }, () => sb.Append(" "));
            var s = sb.ToString();
            if (!s.Contains("/dir"))
                s = String.Format("/dir:\"{0}\" ", Directory.GetCurrentDirectory()) + s;
            return s;
        }


        private void ParseArgs()
        {
            Settings = CompilerSettings.Parse(CommandLineArguments);
        }

        private void WriteArgs()
        {
            Log.WriteLine(Process.GetCurrentProcess().MainModule.FileName + " " + ArgsToString());
        }

        private bool CalculateMissingArgs()
        {
            if (!Settings.Target.IsNotNullOrEmpty())
            {
                switch (Settings.Target.ToLowerInvariant())
                {
                    case "js":
                    case "javascript":
                        Target = new JavaScriptTarget();
                        break;
                    case "java":
                        Target = new JavaTarget();
                        break;
                    default:
                        Log.Error(string.Format("Unknown target '{0}' specified (supported targets: js,javascript,java)", Settings.Target));
                        return false;
                }
            }
            else
            {
                Target = new JavaScriptTarget();
            }

            Target.Compiler = this;

            if (Settings.CurrentDirectory.IsNotNullOrEmpty())
                Directory.SetCurrentDirectory(Settings.CurrentDirectory);
            if (Settings.Output == null)
                Settings.Output = "output" + Target.OutputSuffix;
            if (Settings.ManifestFile == null)
                Settings.ManifestFile = Path.Combine(Path.GetDirectoryName(Settings.Output), Settings.AssemblyName + ".skccache");
            if (Settings.CodeAnalysisFile == null)
                Settings.CodeAnalysisFile = Path.Combine(Path.GetDirectoryName(Settings.Output), Settings.AssemblyName + ".CodeAnalysis");
            if (Settings.SecurityAnalysisFile == null)
                Settings.SecurityAnalysisFile = Path.Combine(Path.GetDirectoryName(Settings.Output), Settings.AssemblyName + ".securityanalysis");

            Defines = Settings.define != null ? Settings.define.Split(';').ToList() : new List<string>();

            return true;
        }

        private bool CheckManifest()
        {
            if (Settings.Rebuild.GetValueOrDefault())
                return false;
            if (!File.Exists(Settings.ManifestFile))
                return false;

            var prev = Manifest.LoadFromFile(Settings.ManifestFile);
            var current = CreateManifest(prev.ExternalFiles.Select(t => t.Filename).ToList());
            Trace.TraceInformation("[{0}] Comparing manifests", DateTime.Now);

            var diff = current.GetManifestDiff(prev);
            if (diff.AreManifestsEqual)
            {
                Log.WriteLine("Code was unmodified - build skipped");
                return true;
            }

            File.Delete(Settings.ManifestFile);
            Log.WriteLine("Reasons for rebuild:\n" + diff.ToString());
            return false;
        }

        #endregion

        #region Manifest

        private Manifest CreateManifest(List<string> externalFiles)
        {
            return new ManifestHelper { Args = Settings, Log = Log, SkcVersion = SkcVersion, SkcFile = typeof(TargetAwareCompiler).Assembly.Location, ExternalFiles = externalFiles }.CreateManifest();
        }

        #endregion

        #region Service

        private const string WindowsServiceName = "SharpKit";

        private void StartWindowsService()
        {
            try
            {
                Log.WriteLine("Starting Service");
                WindowsServiceHelper.StartService(WindowsServiceName);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private void StopWindowsService()
        {
            try
            {
                Log.WriteLine("Stopping Service");
                WindowsServiceHelper.StopService(WindowsServiceName);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private void InstallWindowsService()
        {
            try
            {
                Log.WriteLine("Installing Service");
                WindowsServiceHelper.CreateService(WindowsServiceName, ProcessHelper.CurrentProcessFile.FullName + " /service:windows", "auto");
                StartWindowsService();
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private void UninstallWindowsService()
        {
            try
            {
                StopWindowsService();
                WindowsServiceHelper.DeleteService(WindowsServiceName);
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        public void RunInServiceConsoleMode()
        {
            var server = CreateServer();
            server.Run();
        }

        public void RunInWindowsServiceMode()
        {
            var server = CreateServer();
            var ws = new WindowsService { StartAction = server.Start, StopAction = server.Stop };
            ws.Run();
        }

        private JsonServer CreateServer()
        {
            var compilerService = new CompilerService();
            compilerService.PreLoad();
            var server = new JsonServer { Service = compilerService, Url = CompilerConfiguration.Current.SharpKitServiceUrl };
            return server;
        }

        #endregion

        #region Compilation

        private void ParseCs()
        {
            CustomAttributeProvider = new CustomAttributeProvider();

            OnBeforeParseCs();

            // build a SharpKit project and parse it
            Project = new SkProject
            {
                SourceFiles = Settings.Files,
                Defines = Defines,
                References = Settings.References,
                Log = Log,
                TargetFrameworkVersion = Settings.TargetFrameworkVersion,
                Compiler = this,
                AssemblyName = Settings.AssemblyName,
            };
            Project.Parse();

            var asm = Project.Compilation.MainAssembly;
            if (asm != null && asm.AssemblyName == null)
            {
                throw new NotImplementedException();
                //asm.AssemblyName = Settings.AssemblyName;
            }

            OnAfterParseCs();
        }

        private void ApplyExternalMetadata()
        {
            OnBeforeApplyExternalMetadata();

            ExternalMetadata = Target.BuildExternalMetadata();

            OnAfterApplyExternalMetadata();
        }

        private void ConvertCsToTarget()
        {
            OnBeforeConvertCsToTarget();

            PathMerger = new PathMerger();
            TypeConverter = Target.BuildTypeConverter();
            TypeConverter.Compiler = this;
            TypeConverter.BeforeExportTypes = byFile =>
            {
                var list = new List<ITypeDefinition>();
                foreach (var list2 in byFile.Values)
                {
                    list.AddRange(list2);
                }
                var skFiles = Project.GetNFiles(list);
                Project.ApplyNavigator(skFiles);
            };
            TypeConverter.ConfigureMemberConverter += ConfigureMemberConverter;

            TypeConverter.Process();
            SkFiles = TypeConverter.TargetFiles.Select(Target.CreateSkFile).ToList();

            OnAfterConvertCsToTarget();
        }

        private void ConfigureMemberConverter(IMemberConverter memberConverter)
        {
            memberConverter.BeforeVisitEntity += OnBeforeConvertCsToTargetEntity;
            memberConverter.AfterVisitEntity += OnAfterConvertCsToTargetEntity;
            memberConverter.BeforeConvertCsToTargetAstNode += OnBeforeConvertCsToTargetAstNode;
            memberConverter.AfterConvertCsToTargetAstNode += OnAfterConvertCsToTargetAstNode;
            memberConverter.BeforeConvertCsToTargetResolveResult +=
                OnBeforeConvertCsToTargetResolveResult;
            memberConverter.AfterConvertCsToTargetResolveResult +=
                OnAfterConvertCsToTargetResolveResult;
        }

        private void MergeTargetFiles()
        {
            OnBeforeMergeTargetFiles();

            Target.MergeTargetFiles();

            OnAfterMergeTargetFiles();
        }

        private void InjectTargetCode()
        {
            OnBeforeInjectTargetCode();

            Target.InjectTargetCode();

            OnAfterInjectTargetCode();
        }

        private void OptimizeTargetFiles()
        {
            OnBeforeOptimizeTargetFiles();

            Target.OptimizeTargetFiles();

            OnAfterOptimizeTargetFiles();
        }


        private void SaveTargetFiles()
        {
            OnBeforeSaveTargetFiles();

            Target.SaveTargetFiles();

            OnAfterOptimizeTargetFiles();
        }


        private void EmbedResources()
        {
            OnBeforeEmbedResources();

            Target.EmbedResources();

            OnAfterEmbedResources();
        }

        private void SaveNewManifest()
        {
            OnBeforeSaveNewManifest();

            CreateManifest(Target.ManifestFiles.ToList()).SaveToFile(Settings.ManifestFile);

            OnAfterSaveNewManifest();
        }

        #endregion

        private void LoadPlugins()
        {
            if (Settings.Plugins.IsNullOrEmpty())
                return;
            foreach (var plugin in Settings.Plugins)
            {
                try
                {
                    Log.WriteLine("Loading plugin: " + plugin);
                    var type = Type.GetType(plugin);
                    Log.WriteLine("Found plugin: " + plugin);
                    var obj = Activator.CreateInstance(type, true);
                    Log.WriteLine("Created plugin: " + plugin);
                    Log.WriteLine("Started: Initialize plugin" + plugin);
                    var plugin2 = (ICompilerPlugin)obj;
                    plugin2.Init(this);
                    Log.WriteLine("Finished: Initialize plugin " + plugin);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to load plugin: " + plugin, e);
                }
            }
        }

        private bool Help()
        {
            if (Settings.Help)
            {
                CompilerSettings.GenerateHelp(System.Console.Out);
                return true;
            }
            return false;
        }

        private void CheckForNewVersion()
        {
            Process.Start("http://sharpkit.net/CheckForNewVersion.aspx?v=" + SkcVersion);
        }

        [DebuggerStepThrough]
        private void Time(Action action)
        {
            var stopwatch = new Stopwatch();
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}: Start: ", DateTime.Now, action.Method.Name);
            stopwatch.Start();
            action();
            stopwatch.Stop();
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}: End: {2}ms", DateTime.Now, action.Method.Name, stopwatch.ElapsedMilliseconds);
        }

        [DebuggerStepThrough]
        private T Time2<T>(Func<T> action)
        {
            var stopwatch = new Stopwatch();
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}: Start: ", DateTime.Now, action.Method.Name);
            stopwatch.Start();
            var t = action();
            stopwatch.Stop();
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}: End: {2}ms", DateTime.Now, action.Method.Name, stopwatch.ElapsedMilliseconds);
            return t;
        }

        #region Events

        public event Action BeforeParseCs;
        protected virtual void OnBeforeParseCs()
        {
            Action handler = BeforeParseCs;
            if (handler != null) handler();
        }

        public event Action BeforeApplyExternalMetadata;
        protected virtual void OnBeforeApplyExternalMetadata()
        {
            Action handler = BeforeApplyExternalMetadata;
            if (handler != null) handler();
        }

        public event Action BeforeConvertCsToTarget;
        protected virtual void OnBeforeConvertCsToTarget()
        {
            Action handler = BeforeConvertCsToTarget;
            if (handler != null) handler();
        }

        public event Action BeforeMergeTargetFiles;
        protected virtual void OnBeforeMergeTargetFiles()
        {
            Action handler = BeforeMergeTargetFiles;
            if (handler != null) handler();
        }

        public event Action BeforeInjectTargetCode;
        protected virtual void OnBeforeInjectTargetCode()
        {
            Action handler = BeforeInjectTargetCode;
            if (handler != null) handler();
        }

        public event Action BeforeOptimizeTargetFiles;
        protected virtual void OnBeforeOptimizeTargetFiles()
        {
            Action handler = BeforeOptimizeTargetFiles;
            if (handler != null) handler();
        }

        public event Action BeforeSaveTargetFiles;
        protected virtual void OnBeforeSaveTargetFiles()
        {
            Action handler = BeforeSaveTargetFiles;
            if (handler != null) handler();
        }

        public event Action BeforeEmbedResources;
        protected virtual void OnBeforeEmbedResources()
        {
            Action handler = BeforeEmbedResources;
            if (handler != null) handler();
        }

        public event Action BeforeSaveNewManifest;
        protected virtual void OnBeforeSaveNewManifest()
        {
            Action handler = BeforeSaveNewManifest;
            if (handler != null) handler();
        }

        public event Action BeforeExit;
        protected virtual void OnBeforeExit()
        {
            Action handler = BeforeExit;
            if (handler != null) handler();
        }

        public event Action AfterParseCs;
        protected virtual void OnAfterParseCs()
        {
            Action handler = AfterParseCs;
            if (handler != null) handler();
        }

        public event Action AfterApplyExternalMetadata;
        protected virtual void OnAfterApplyExternalMetadata()
        {
            Action handler = AfterApplyExternalMetadata;
            if (handler != null) handler();
        }

        public event Action AfterConvertCsToTarget;
        protected virtual void OnAfterConvertCsToTarget()
        {
            Action handler = AfterConvertCsToTarget;
            if (handler != null) handler();
        }

        public event Action AfterMergeTargetFiles;
        protected virtual void OnAfterMergeTargetFiles()
        {
            Action handler = AfterMergeTargetFiles;
            if (handler != null) handler();
        }

        public event Action AfterInjectTargetCode;
        protected virtual void OnAfterInjectTargetCode()
        {
            Action handler = AfterInjectTargetCode;
            if (handler != null) handler();
        }

        public event Action AfterOptimizeTargetFiles;
        protected virtual void OnAfterOptimizeTargetFiles()
        {
            Action handler = AfterOptimizeTargetFiles;
            if (handler != null) handler();
        }

        public event Action AfterSaveTargetFiles;
        protected virtual void OnAfterSaveTargetFiles()
        {
            Action handler = AfterSaveTargetFiles;
            if (handler != null) handler();
        }

        public event Action AfterEmbedResources;
        protected virtual void OnAfterEmbedResources()
        {
            Action handler = AfterEmbedResources;
            if (handler != null) handler();
        }

        public event Action AfterSaveNewManifest;
        protected virtual void OnAfterSaveNewManifest()
        {
            Action handler = AfterSaveNewManifest;
            if (handler != null) handler();
        }

        public event Action<IEntity> BeforeConvertCsToTargetEntity;
        protected virtual void OnBeforeConvertCsToTargetEntity(IEntity obj)
        {
            Action<IEntity> handler = BeforeConvertCsToTargetEntity;
            if (handler != null) handler(obj);
        }

        public event Action<IEntity, ITargetNode> AfterConvertCsToTargetEntity;
        protected virtual void OnAfterConvertCsToTargetEntity(IEntity arg1, ITargetNode arg2)
        {
            Action<IEntity, ITargetNode> handler = AfterConvertCsToTargetEntity;
            if (handler != null) handler(arg1, arg2);
        }

        public event Action<AstNode> BeforeConvertCsToTargetAstNode;
        protected virtual void OnBeforeConvertCsToTargetAstNode(AstNode obj)
        {
            Action<AstNode> handler = BeforeConvertCsToTargetAstNode;
            if (handler != null) handler(obj);
        }

        public event Action<AstNode, ITargetNode> AfterConvertCsToTargetAstNode;
        protected virtual void OnAfterConvertCsToTargetAstNode(AstNode arg1, ITargetNode arg2)
        {
            Action<AstNode, ITargetNode> handler = AfterConvertCsToTargetAstNode;
            if (handler != null) handler(arg1, arg2);
        }

        public event Action<ResolveResult> BeforeConvertCsToTargetResolveResult;
        protected virtual void OnBeforeConvertCsToTargetResolveResult(ResolveResult obj)
        {
            Action<ResolveResult> handler = BeforeConvertCsToTargetResolveResult;
            if (handler != null) handler(obj);
        }

        public event Action<ResolveResult, ITargetNode> AfterConvertCsToTargetResolveResult;
        protected virtual void OnAfterConvertCsToTargetResolveResult(ResolveResult arg1, ITargetNode arg2)
        {
            Action<ResolveResult, ITargetNode> handler = AfterConvertCsToTargetResolveResult;
            if (handler != null) handler(arg1, arg2);
        }

        #endregion
    }
}
