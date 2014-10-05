using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Mirrored.SharpKit.JavaScript;
using ICSharpCode.NRefactory.TypeSystem;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Globalization;
using Mono.CSharp;
using SharpKit.Compiler.Ast;
using SharpKit.Compiler.JavaScript;
using SharpKit.JavaScript.Ast;
using SharpKit.Compiler.SourceMapping;
using Mono.Cecil;
using System.Collections;
using System.Xml.Linq;
using ICSharpCode.NRefactory.CSharp;
using SharpKit.Targets;
using SharpKit.Targets.Ast;
using SharpKit.Targets.JavaScript;
using SharpKit.Utils.Http;
using Corex.Helpers;
using Corex.IO.Tools;
using SharpKit.Compiler.CsToJs;
using ITypeDefinition = ICSharpCode.NRefactory.TypeSystem.ITypeDefinition;

namespace SharpKit.Compiler
{
    class CompilerTool : ICompiler
    {
        public Dictionary<string, object> TargetData { get; private set; }

        public ICompilation Compilation
        {
            get { return Project.Compilation; }
        }

        public CompilerTool()
        {
            TargetData = new Dictionary<string, object>();
            EmbeddedResourceFiles = new List<string>();
            CompilerConfiguration.LoadCurrent();
            NativeAnonymousDelegateSupportStatement = Js.CodeStatement(NativeAnonymousDelegateSupportCode);
            NativeDelegateSupportStatement = Js.CodeStatement(NativeDelegateSupportCode);
            NativeInheritanceSupportStatement = Js.CodeStatement(NativeInheritanceSupportCode);
            NativeExtensionDelegateSupportStatement = Js.CodeStatement(NativeExtensionDelegateSupportCode);
            CombineDelegatesSupportStatement = Js.CodeStatement(CombineDelegatesSupportCode);
            RemoveDelegateSupportStatement = Js.CodeStatement(RemoveDelegateSupportCode);
            CreateMulticastDelegateFunctionSupportStatement = Js.CodeStatement(CreateMulticastDelegateFunctionSupportCode);
            CreateExceptionSupportStatement = Js.CodeStatement(CreateExceptionSupportCode);
            CreateAnonymousObjectSupportStatement = Js.CodeStatement(CreateAnonymousObjectSupportCode);

            CodeInjections = new List<CodeInjection>
            {
                new CodeInjection
                {
                    JsCode = NativeAnonymousDelegateSupportCode,
                    JsStatement = NativeAnonymousDelegateSupportStatement,
                    FunctionName = "$CreateAnonymousDelegate",
                },
                new CodeInjection
                {
                    JsCode = NativeDelegateSupportCode,
                    JsStatement = NativeDelegateSupportStatement,
                    FunctionName = "$CreateDelegate",
                },
                new CodeInjection
                {
                    JsCode = NativeInheritanceSupportCode,
                    JsStatement = NativeInheritanceSupportStatement,
                    FunctionName = "$Inherit",
                },
                new CodeInjection
                {
                    JsCode = NativeExtensionDelegateSupportCode,
                    JsStatement = NativeExtensionDelegateSupportStatement,
                    FunctionName = "$CreateExtensionDelegate",
                },
                new CodeInjection
                {
                    JsCode = CreateExceptionSupportCode,
                    JsStatement = CreateExceptionSupportStatement,
                    FunctionName = "$CreateException",
                },
                new CodeInjection
                {
                    JsCode = CreateMulticastDelegateFunctionSupportCode,
                    JsStatement = CreateMulticastDelegateFunctionSupportStatement,
                    FunctionName = "$CreateMulticastDelegateFunction",
                },
                new CodeInjection
                {
                    JsCode = CombineDelegatesSupportCode,
                    JsStatement = CombineDelegatesSupportStatement,
                    FunctionName = "$CombineDelegates",
                },
                new CodeInjection
                {
                    JsCode = RemoveDelegateSupportCode,
                    JsStatement = RemoveDelegateSupportStatement,
                    FunctionName = "$RemoveDelegate",
                },
                new CodeInjection
                {
                    JsCode = CreateAnonymousObjectSupportCode,
                    JsStatement = CreateAnonymousObjectSupportStatement,
                    FunctionName = "$CreateAnonymousObject",
                },
          };
            var dep1 = CodeInjections.Where(t => t.FunctionName == "$CreateMulticastDelegateFunction").FirstOrDefault();

            AddCodeInjectionDependency("$CombineDelegates", "$CreateMulticastDelegateFunction");
            AddCodeInjectionDependency("$RemoveDelegate", "$CreateMulticastDelegateFunction");


            foreach (var ta in TypedArrays)
            {
                var define = Js.If(Js.Typeof(Js.Member(ta)).Equal(Js.Value("undefined")), Js.Var(ta, Js.Member("Array")).Statement());
                CodeInjections.Add(new CodeInjection
                {
                    FunctionName = ta,
                    JsStatement = define,
                });
            }
        }


        #region Properties
        //public static CompilerTool Current { get; set; }
        public CompilerSettings Settings { get; set; }
        public List<SkFile> SkFiles { get; set; }
        public CompilerLogger Log { get; set; }
        public SkProject Project { get; set; }
        public List<string> Defines { get; set; }
        public ICsExternalMetadata CsExternalMetadata { get; set; }
        public string SkcVersion { get; set; }
        public string[] CommandLineArguments { get; set; }
        public bool Debug { get; set; }


        public JsStatement CombineDelegatesSupportStatement { get; set; }
        public JsStatement RemoveDelegateSupportStatement { get; set; }
        public JsStatement CreateMulticastDelegateFunctionSupportStatement { get; set; }
        public SkSourceMappingGenerator SourceMapsGenerator { get; set; }
        public SkFile CodeInjectionFile { get; set; }
        public List<string> EmbeddedResourceFiles { get; set; }
        public string VersionKey { get; set; }
        public JsFileMerger JsFileMerger { get; set; }
        public PathMerger PathMerger { get; set; }

        #endregion

        #region Fields

        JsTypeConverter TypeConverter;
        ITypeConverter ICompiler.TypeConverter
        {
            get { return TypeConverter; }
        }


        JsStatement NativeInheritanceSupportStatement;
        JsStatement NativeExtensionDelegateSupportStatement;
        JsStatement CreateExceptionSupportStatement;
        JsStatement NativeDelegateSupportStatement;
        JsStatement NativeAnonymousDelegateSupportStatement;

        public static string[] TypedArrays = new[]
            {
                "Int8Array",
                "Uint8Array",
                "Int16Array",
                "Uint16Array",
                "Int32Array",
                "Uint32Array",
                "Float32Array",
                "Float64Array",
            };
        List<CodeInjection> CodeInjections;

        //Skc5CacheData Skc5CacheData;
        //string Skc5CacheDataFilename;

        #endregion

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
            if (BeforeExit != null)
                BeforeExit();
            return x;
        }
        int InternalRun()
        {
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                SkcVersion = typeof(CompilerTool).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
                VersionKey = this.SkcVersion + "||" + File.GetLastWriteTime(Process.GetCurrentProcess().MainModule.FileName).ToBinary();


                Time(SaveLastInputArgs);
                WriteArgs();
                Time(ParseArgs);
                if (Settings.LastArgs)
                {
                    var tokenizer = new ToolArgsTokenizer();
                    var args = tokenizer.Tokenize(LastArgs);
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

                if (!Settings.AddBuildTarget.IsNullOrEmpty())
                {
                    Time(AddBuildTarget);
                    return 0;
                }

                if (Help())
                    return 0;
                if (Settings.Service != null)
                {
                    var action = Settings.Service.ToLower();
                    if (action == "start")
                    {
                        StartWindowsService();
                    }
                    else if (action == "stop")
                    {
                        StopWindowsService();
                    }
                    else if (action == "restart")
                    {
                        Try(StopWindowsService);
                        StartWindowsService();
                    }
                    else if (action == "install")
                    {
                        InstallWindowsService();
                    }
                    else if (action == "uninstall")
                    {
                        UninstallWindowsService();
                    }
                    else if (action == "reinstall")
                    {
                        Log.WriteLine("Reinstalling Service");
                        Try(UninstallWindowsService);
                        InstallWindowsService();
                    }
                    else if (action == "console")
                    {
                        Log.WriteLine("Starting Console Service");
                        RunInServiceConsoleMode();
                    }
                    else if (action == "windows")
                    {
                        Log.WriteLine("Starting Windows Service");
                        RunInWindowsServiceMode();
                    }
                    return 0;
                }

                Time(CalculateMissingArgs);
                if (Time2(CheckManifest))
                    return 0;
                Time(VerifyNativeImage);
                Time(LoadPlugins);

                Time(ParseCs);
                Time(ApplyExternalMetadata);
                Time(ConvertCsToJs);
                Time(MergeJsFiles);
                //Time(ValidateUnits));
                Time(InjectJsCode);
                Time(OptimizeJsFiles);
                Time(SaveJsFiles);
                Time(EmbedResources);
                //Time(GenerateSourceMappings);
                if (Log.Items.Where(t => t.Type == CompilerLogItemType.Error).FirstOrDefault() != null)
                    return -1;
                Time(SaveNewManifest);
                return 0;
            }
            catch (Exception e)
            {
                Log.Log(e);
                Log.Console.WriteLine(e);
                return -1;
            }
        }

        private void StopWindowsService()
        {
            Log.WriteLine("Stopping Service");
            WindowsServiceHelper.StopService(WindowsServiceName);
        }

        private void StartWindowsService()
        {
            Log.WriteLine("Starting Service");
            WindowsServiceHelper.StartService(WindowsServiceName);
        }

        string WindowsServiceName = "SharpKit";

        private void UninstallWindowsService()
        {
            Try(StopWindowsService);
            WindowsServiceHelper.DeleteService(WindowsServiceName);
        }

        bool Try(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                Log.WriteLine(e.ToString());
                return false;
            }
        }
        private void InstallWindowsService()
        {
            Log.WriteLine("Installing Service");
            WindowsServiceHelper.CreateService(WindowsServiceName, ProcessHelper.CurrentProcessFile.FullName + " /service:windows", "auto");
            StartWindowsService();
        }

        bool Help()
        {
            if (Settings.Help)
            {
                CompilerSettings.GenerateHelp(System.Console.Out);
                return true;
            }
            return false;
        }
        public void RunInServiceConsoleMode()
        {
            var server = CreateServer();
            server.Run();
        }

        private JsonServer CreateServer()
        {
            var compilerService = new CompilerService();
            compilerService.PreLoad();
            var server = new JsonServer { Service = compilerService, Url = CompilerConfiguration.Current.SharpKitServiceUrl };
            //server.DeserializeFromQueryStringOverride += e =>
            //    {
            //        if (e.Type == typeof(CompileRequest))
            //        {
            //            var req = new CompileRequest();
            //            var args = server.DeserializeFromQueryString(typeof(CompilerToolArgs), e.QueryString) as CompilerToolArgs;
            //            req.Settings = args;
            //            e.Handled = true;
            //            e.Result = req;
            //        }
            //    };
            return server;
        }


        public void RunInWindowsServiceMode()
        {
            var server = CreateServer();
            var ws = new WindowsService();
            ws.StartAction = () => server.Start();
            ws.StopAction = () => server.Stop();
            ws.Run();
        }


        void CheckForNewVersion()
        {
            Process.Start("http://sharpkit.net/CheckForNewVersion.aspx?v=" + SkcVersion);
        }

        void AddBuildTarget()
        {
            bool nuget = false;
            var file = Settings.AddBuildTarget;
            if (file.EndsWith(";nuget"))
            {
                nuget = true;
                file = file.Replace(";nuget", "");
            }
            var doc = XDocument.Parse(File.ReadAllText(file, Encoding.UTF8));

            if (((XElement)doc.LastNode).Nodes().Count((n) => n is XElement ? ((XElement)n).Name.LocalName == "Import" && ((XElement)n).LastAttribute.Value.Contains("SharpKit") : false) > 0)
            {
                Log.WriteLine("Already registered");
                return;
            }

            var importNode = (XElement)((XElement)doc.LastNode).Nodes().Last(
                (n) =>
                {
                    return n is XElement ? ((XElement)n).Name.LocalName == "Import" : false;
                });

            var path = Path.Combine("$(MSBuildBinPath)", "SharpKit", "5", "SharpKit.Build.targets");
            if (nuget)
                path = Path.Combine("$(SolutionDir)", "packages", "SharpKit.5.0.0", "tools", "SharpKit.Build.targets");
            importNode.AddAfterSelf(new XElement(XName.Get("Import", importNode.Name.NamespaceName), new XAttribute("Project", path)));

            doc.Save(file);
        }

        void LoadPlugins()
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

        #region Manifest

        void SaveNewManifest()
        {

            TriggerEvent(BeforeSaveNewManifest);
            CreateManifest(JsFileMerger.ExternalFiles.Select(t => t.TargetFile.Filename).Concat(EmbeddedResourceFiles).ToList()).SaveToFile(Settings.ManifestFile);

            TriggerEvent(AfterSaveNewManifest);
        }

        Manifest CreateManifest(List<string> externalFiles)
        {
            return new ManifestHelper { Args = Settings, Log = Log, SkcVersion = SkcVersion, SkcFile = typeof(CompilerTool).Assembly.Location, ExternalFiles = externalFiles }.CreateManifest();
        }

        #endregion

        #region ParseArgs

        bool CheckManifest()
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
            else
            {
                File.Delete(Settings.ManifestFile);
                Log.WriteLine("Reasons for rebuild:\n" + diff.ToString());
            }
            return false;
        }

        void ParseArgs()
        {
            Settings = CompilerSettings.Parse(CommandLineArguments);
        }

        void WriteArgs()
        {
            Log.WriteLine(Process.GetCurrentProcess().MainModule.FileName + " " + ArgsToString());
        }

        string LastArgs;
        void SaveLastInputArgs()
        {
            var file = Process.GetCurrentProcess().MainModule.FileName.ToFileInfo().Directory.GetFile("prms.txt");
            if (file.Exists)
                LastArgs = file.ReadAllText();
            var s = ArgsToString();
            try
            {
                file.WriteAllText(s);
            }
            catch
            {
            }
        }

        string ArgsToString()
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

        void CalculateMissingArgs()
        {
            if (Settings.CurrentDirectory.IsNotNullOrEmpty())
                Directory.SetCurrentDirectory(Settings.CurrentDirectory);
            if (Settings.Output == null)
                Settings.Output = "output.js";
            if (Settings.ManifestFile == null)
                Settings.ManifestFile = Path.Combine(Path.GetDirectoryName(Settings.Output), Settings.AssemblyName + ".skccache");
            if (Settings.CodeAnalysisFile == null)
                Settings.CodeAnalysisFile = Path.Combine(Path.GetDirectoryName(Settings.Output), Settings.AssemblyName + ".CodeAnalysis");
            if (Settings.SecurityAnalysisFile == null)
                Settings.SecurityAnalysisFile = Path.Combine(Path.GetDirectoryName(Settings.Output), Settings.AssemblyName + ".securityanalysis");

            Defines = Settings.define != null ? Settings.define.Split(';').ToList() : new List<string>();

        }

        #endregion

        void ParseCs()
        {
            TriggerEvent(BeforeParseCs);
            _CustomAttributeProvider = new CustomAttributeProvider();
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

            TriggerEvent(AfterParseCs);
        }

        void ApplyExternalMetadata()
        {

            TriggerEvent(BeforeApplyExternalMetadata);
            CsExternalMetadata = new JsExternalMetadata { Project = Project, Log = Log };
            CsExternalMetadata.Process();

            TriggerEvent(AfterApplyExternalMetadata);
        }

        void ConvertCsToJs()
        {
            TriggerEvent(BeforeConvertCsToTarget);
            PathMerger = new PathMerger();
            TypeConverter = new JsTypeConverter
            {
                Compiler = this,

                ExternalMetadata = CsExternalMetadata,
                AssemblyName = Settings.AssemblyName,
                BeforeExportTypes = byFile =>
                    {
                        var list = new List<ITypeDefinition>();
                        foreach (var list2 in byFile.Values)
                        {
                            list.AddRange(list2);
                        }
                        var skFiles = Project.GetNFiles(list);
                        Project.ApplyNavigator(skFiles);
                    }
            };
            TypeConverter.ConfigureMemberConverter += JsModelImporter_ConfigureJsTypeImporter;
            var att = GetJsExportAttribute();
            if (att != null)
            {
                TypeConverter.ExportComments = att.ExportComments;
                //LongFunctionNames = att.LongFunctionNames;
                //Minify = att.Minify;
                //EnableProfiler = att.EnableProfiler;
            }


            TypeConverter.Process();
            SkFiles = TypeConverter.TargetFiles.Select(ToSkJsFile).ToList();

            TriggerEvent(AfterConvertCsToTarget);
        }

        private SkFile ToSkJsFile(TargetFile t)
        {
            return new SkJsFile { TargetFile = (JsFile)t, Compiler = this };
        }

        void JsModelImporter_ConfigureJsTypeImporter(IMemberConverter obj)
        {
            obj.BeforeVisitEntity += me =>
            {
                if (BeforeConvertCsToTargetEntity != null)
                    BeforeConvertCsToTargetEntity(me);
            };
            obj.AfterVisitEntity += (me, node) =>
            {
                if (AfterConvertCsToTargetEntity != null)
                    AfterConvertCsToTargetEntity(me, node);
            };
            obj.BeforeConvertCsToTargetAstNode += node =>
            {
                if (BeforeConvertCsToTargetAstNode != null)
                    BeforeConvertCsToTargetAstNode(node);
            };
            obj.AfterConvertCsToTargetAstNode += (node, node2) =>
            {
                if (AfterConvertCsToTargetAstNode != null)
                    AfterConvertCsToTargetAstNode(node, node2);
            };
            obj.BeforeConvertCsToTargetResolveResult += res =>
            {
                if (BeforeConvertCsToTargetResolveResult != null)
                    BeforeConvertCsToTargetResolveResult(res);
            };
            obj.AfterConvertCsToTargetResolveResult += (res, node) =>
            {
                if (AfterConvertCsToTargetResolveResult != null)
                    AfterConvertCsToTargetResolveResult(res, node);
            };
        }



        void MergeJsFiles()
        {

            TriggerEvent(BeforeMergeTargetFiles);
            JsFileMerger = new JsFileMerger { Project = Project, Files = SkFiles.OfType<SkJsFile>().ToList(), Log = Log, Compiler = this };
            Time(GenerateCodeInjectionFile);

            JsFileMerger.MergeFiles();
            if (Settings.OutputGeneratedJsFile.IsNotNullOrEmpty())
            {
                var file = JsFileMerger.GetJsFile(Settings.OutputGeneratedJsFile, false);
                JsFileMerger.MergeFiles(file, SkFiles.OfType<SkJsFile>().Where(t => t != file).ToList());
                SkFiles.Clear();
                SkFiles.Add(file);
            }

            ApplyJsMinifyAndSourceMap();


            TriggerEvent(AfterMergeTargetFiles);
        }

        void ApplyJsMinifyAndSourceMap()
        {
            var att = Project.Compilation.MainAssembly.GetMetadata<JsExportAttribute>();
            if (att != null)
            {
                if (att.Minify)
                    SkFiles.OfType<SkJsFile>().ForEach(t => t.Minify = true);
                if (att.GenerateSourceMaps)
                    SkFiles.OfType<SkJsFile>().ForEach(t => t.GenerateSourceMap = true);
            }
        }

        #region Code Injection
        void AddCodeInjectionDependency(string funcName, string depFuncName)
        {
            var func = CodeInjections.Where(t => t.FunctionName == funcName).FirstOrDefault();
            var dep = CodeInjections.Where(t => t.FunctionName == depFuncName).FirstOrDefault();
            if (func != null && dep != null)
                func.Dependencies.Add(dep);
        }

        JsUnit CreateSharpKitHeaderUnit()
        {
            var unit = new JsUnit { Statements = new List<JsStatement>() };
            var att = GetJsExportAttribute();
            if (att == null || !att.OmitSharpKitHeaderComment)
            {
                var txt = " Generated by SharpKit 5 v" + SkcVersion + " ";
                if (att != null && att.AddTimeStampInSharpKitHeaderComment)
                    txt += "on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ";
                unit.Statements.Add(new JsCommentStatement { Text = txt });
            }
            if (att != null && att.UseStrict)
            {
                unit.Statements.Add(new JsUseStrictStatement());
            }

            return unit;
        }

        HashSet<string> CheckHelperMethodsUsage(SkJsFile file, string[] methods)
        {
            var usage = new HashSet<string>();
            if (methods.Length == 0)
                return usage;
            var list = new HashSet<string>(methods);
            foreach (var unit in file.TargetFile.Units)
            {
                foreach (var node in unit.Descendants<JsInvocationExpression>())
                {
                    var me = node.Member as JsMemberExpression;
                    if (me != null && list.Remove(me.Name))
                    {
                        usage.Add(me.Name);
                        if (list.Count == 0)
                            break;
                    }
                }
            }
            return usage;
        }

        void GenerateCodeInjectionFile()
        {
            var att = Project.Compilation.MainAssembly.GetMetadata<JsExportAttribute>();
            if (att != null && att.CodeInjectionFilename.IsNotNullOrEmpty())
            {
                CodeInjectionFile = GetCreateSkJsFile(att.CodeInjectionFilename);
                var headerUnit = CreateSharpKitHeaderUnit();
                foreach (var ci in CodeInjections)
                {
                    headerUnit.Statements.Add(ci.JsStatement);
                }
                ((SkJsFile)CodeInjectionFile).TargetFile.Units.Insert(0, headerUnit);
            }
        }

        SkFile GetCreateSkJsFile(string filename)
        {
            return JsFileMerger.GetJsFile(filename, false);
            //return SkFiles.Where(t => t.JsFile.Filename.EqualsIgnoreCase(filename)).FirstOrDefault();
        }

        void InjectJsCode()
        {

            TriggerEvent(BeforeInjectTargetCode);
            if (SkFiles.IsNotNullOrEmpty())
            {
                var helperMethods = CodeInjections.Select(t => t.FunctionName).ToArray();
                foreach (var file in SkFiles.OfType<SkJsFile>())
                {
                    if (file.TargetFile == null || file.TargetFile.Units.IsNullOrEmpty())
                        continue;
                    if (file == CodeInjectionFile)
                        continue;
                    var jsFile = file.TargetFile;
                    var ext = Path.GetExtension(jsFile.Filename).ToLower();
                    if (ext == ".js")
                    {
                        var headerUnit = CreateSharpKitHeaderUnit();
                        if (CodeInjectionFile == null)
                        {
                            var usage = CheckHelperMethodsUsage(file, helperMethods);
                            var handled = new HashSet<CodeInjection>();
                            foreach (var funcName in usage)
                            {
                                var ci = CodeInjections.Where(t => t.FunctionName == funcName).FirstOrDefault();
                                foreach (var ci2 in ci.SelfAndDependencies())
                                {
                                    if (handled.Add(ci2))
                                        headerUnit.Statements.Add(ci2.JsStatement.Clone());
                                }
                            }
                        }
                        jsFile.Units.Insert(0, headerUnit);
                    }

                }
            }

            TriggerEvent(AfterInjectTargetCode);
        }

        #endregion

        #region ValidateUnits
        void ValidateUnits()
        {
            SkFiles.OfType<SkJsFile>().Select(t => t.TargetFile).ToList().ForEach(ValidateUnits);
        }
        void ValidateUnits(JsFile file)
        {
            file.Units.ForEach(ValidateUnit);
        }
        void ValidateUnit(JsNode node)
        {
            if (node == null)
                throw new NotImplementedException();
            var children = node.Children().ToList();
            children.ForEach(ValidateUnit);
        }

        #endregion

        #region Optimize

        void OptimizeClrJsTypesArrayVerification()
        {
            if (TypeConverter == null || TypeConverter.ClrConverter == null)
                return;
            var st = TypeConverter.ClrConverter.VerifyJsTypesArrayStatement;
            if (st == null)
                return;
            if (SkFiles.IsNullOrEmpty())
                return;
            foreach (var file in SkFiles.OfType<SkJsFile>())
            {
                if (file.TargetFile == null || file.TargetFile.Units == null)
                    continue;
                foreach (var unit in file.TargetFile.Units)
                {
                    if (unit.Statements == null)
                        continue;
                    unit.Statements.RemoveDoubles(t => t.Annotation<VerifyJsTypesArrayStatementAnnotation>() != null);
                }
            }
        }

        void OptimizeJsFiles()
        {

            TriggerEvent(BeforeOptimizeTargetFiles);
            OptimizeClrJsTypesArrayVerification();
            OptimizeNamespaceVerification();

            TriggerEvent(AfterOptimizeTargetFiles);
        }

        void OptimizeNamespaceVerification()
        {
            if (SkFiles.IsNullOrEmpty())
                return;
            foreach (var file in SkFiles.OfType<SkJsFile>())
            {
                if (file.TargetFile == null || file.TargetFile.Units.IsNullOrEmpty())
                    continue;
                foreach (var unit in file.TargetFile.Units)
                {
                    OptimizeNamespaceVerification(unit);
                }
            }
        }

        string GetNamespaceVerification(JsStatement st)
        {
            var ex = st.Annotation<NamespaceVerificationAnnotation>();
            if (ex != null)
                return ex.Namespace;
            return null;
        }

        void OptimizeNamespaceVerification(JsUnit unit)
        {
            if (unit.Statements.IsNullOrEmpty())
                return;
            unit.Statements.RemoveDoublesByKey(t => GetNamespaceVerification(t));

        }
        #endregion

        void SaveJsFiles()
        {
            TriggerEvent(BeforeSaveTargetFiles);
            var att = GetJsExportAttribute();
            string format = null;
            if (att != null)
                format = att.JsCodeFormat;
            foreach (var file in SkFiles)
            {
                file.Format = format;
                file.Save();
            }
            TriggerEvent(AfterSaveTargetFiles);
        }

        void EmbedResources()
        {
            TriggerEvent(BeforeEmbedResources);
            var atts = Project.Compilation.MainAssembly.GetMetadatas<JsEmbeddedResourceAttribute>().ToList();
            if (atts.IsNotNullOrEmpty())
            {
                var asmFilename = Settings.Output;
                Log.WriteLine("Loading assembly {0}", asmFilename);
                var asm = ModuleDefinition.ReadModule(asmFilename);
                var changed = false;
                foreach (var att in atts)
                {
                    if (att.Filename.IsNullOrEmpty())
                        throw new CompilerException(att.SourceAttribute, "JsEmbeddedResourceAttribute.Filename must be set");
                    EmbeddedResourceFiles.Add(att.Filename);
                    var resName = att.ResourceName ?? att.Filename;
                    Log.WriteLine("Embedding {0} -> {1}", att.Filename, resName);
                    var res = new EmbeddedResource(resName, ManifestResourceAttributes.Public, File.ReadAllBytes(att.Filename));
                    var res2 = asm.Resources.Where(t => t.Name == res.Name).OfType<EmbeddedResource>().FirstOrDefault();
                    if (res2 == null)
                    {
                        asm.Resources.Add(res);
                    }
                    else
                    {
                        IStructuralEquatable data2 = res2.GetResourceData();
                        IStructuralEquatable data = res.GetResourceData();

                        if (data.Equals(data2))
                            continue;
                        asm.Resources.Remove(res2);
                        asm.Resources.Add(res);
                    }
                    changed = true;

                }
                if (changed)
                {
                    var prms = new WriterParameters { };//TODO:StrongNameKeyPair = new StrongNameKeyPair("Foo.snk") };
                    var snkFile = Settings.NoneFiles.Where(t => t.EndsWith(".snk", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                    if (snkFile != null)
                    {
                        Log.WriteLine("Signing assembly with strong-name keyfile: {0}", snkFile);
                        prms.StrongNameKeyPair = new StrongNameKeyPair(snkFile);
                    }
                    Log.WriteLine("Saving assembly {0}", asmFilename);
                    asm.Write(asmFilename, prms);
                }
            }
            TriggerEvent(AfterEmbedResources);

        }

        #region NativeImage

        bool ShouldCreateNativeImage()
        {
            if (Settings != null && Settings.CreateNativeImage)
                return true;
            return false;
            //if (Debug)
            //{
            //    Log.WriteLine("ShouldCreateNativeImage? no - debug mode");
            //    return false;
            //}
            //if (!CompilerConfiguration.Current.CreateNativeImage)
            //{
            //    Log.WriteLine("ShouldCreateNativeImage? no - config says not to");
            //    return false;
            //}
            //var dir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            //Skc5CacheDataFilename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SharpKit\\5\\skc5.exe.cache");

            //Skc5CacheData = new Skc5CacheData();
            //if (!File.Exists(Skc5CacheDataFilename))
            //{
            //    Log.WriteLine("ShouldCreateNativeImage? yes - cache file not found");
            //    return true;
            //}
            //Skc5CacheData.Load(Skc5CacheDataFilename);
            //if (VersionKey != Skc5CacheData.VersionKey)
            //{
            //    if (Skc5CacheData.NGenRetries.GetValueOrDefault() > 3)
            //    {
            //        Log.WriteLine("ShouldCreateNativeImage? false - NGenRetries>3");
            //        return false;
            //    }
            //    Log.WriteLine("ShouldCreateNativeImage? true - NGenRetries<=3");
            //    return true;
            //}
            //Log.WriteLine("ShouldCreateNativeImage? false - already created");
            //return false;
        }
        public void VerifyNativeImage()
        {
            try
            {
                if (ShouldCreateNativeImage())
                {
                    CreateNativeImage();
                    //Skc5CacheData.NGenRetries = 0;
                    //Skc5CacheData.CreatedNativeImage = true;
                    //Skc5CacheData.VersionKey = VersionKey;
                    //Skc5CacheData.Save(Skc5CacheDataFilename);
                }
            }
            catch (Exception e)
            {
                //Skc5CacheData.NGenRetries++;
                //Log.Debug("VerifyNativeImage failed: " + e);
                //try
                //{
                //    Skc5CacheData.Save(Skc5CacheDataFilename);
                //}
                //catch (Exception ee)
                //{
                //    Log.Debug("Save skc.exe.cache failed: " + ee);
                //    Log.Warn(ee.ToString());
                //}
                Log.Warn(e.ToString());
            }
        }
        void CreateNativeImage()
        {
            Log.WriteLine("CreateNativeImage started");
            var windir = Environment.ExpandEnvironmentVariables("%windir%");
            var ngen = Path.Combine(windir, @"Microsoft.NET\Framework\v4.0.30319\ngen.exe");
            var skc = Process.GetCurrentProcess().MainModule.FileName;
            var args = "install " + skc;
            var pi = new ProcessStartInfo { CreateNoWindow = true };

            var p = Process.Start(ngen, args);
            p.WaitForExit();
            if (p.ExitCode != 0)
                throw new Exception("CreateNativeImage failed - command=" + ngen + " " + args);
            Log.WriteLine("CreateNativeImage finished");
        }
        #endregion

        #region ICompiler Members

        public event Action BeforeParseCs;
        public event Action BeforeApplyExternalMetadata;
        public event Action BeforeConvertCsToTarget;
        public event Action BeforeMergeTargetFiles;
        public event Action BeforeInjectTargetCode;
        public event Action BeforeOptimizeTargetFiles;
        public event Action BeforeSaveTargetFiles;
        public event Action BeforeEmbedResources;
        public event Action BeforeSaveNewManifest;

        public event Action AfterParseCs;
        public event Action AfterApplyExternalMetadata;
        public event Action AfterConvertCsToTarget;
        public event Action AfterMergeTargetFiles;
        public event Action AfterInjectTargetCode;
        public event Action AfterOptimizeTargetFiles;
        public event Action AfterSaveTargetFiles;
        public event Action AfterEmbedResources;
        public event Action AfterSaveNewManifest;


        public ICompilation CsCompilation
        {
            get
            {
                if (Project == null)
                    return null;
                return Project.Compilation;
            }
        }

        #endregion

        #region Utils

        [DebuggerStepThrough]
        void Time(Action action)
        {
            var stopwatch = new Stopwatch();
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}: Start: ", DateTime.Now, action.Method.Name);
            stopwatch.Start();
            action();
            stopwatch.Stop();
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}: End: {2}ms", DateTime.Now, action.Method.Name, stopwatch.ElapsedMilliseconds);
        }

        [DebuggerStepThrough]
        T Time2<T>(Func<T> action)
        {
            var stopwatch = new Stopwatch();
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}: Start: ", DateTime.Now, action.Method.Name);
            stopwatch.Start();
            var t = action();
            stopwatch.Stop();
            Log.WriteLine("{0:HH:mm:ss.fff}: {1}: End: {2}ms", DateTime.Now, action.Method.Name, stopwatch.ElapsedMilliseconds);
            return t;
        }

        void TriggerEvent(Action ev)
        {
            if (ev != null)
                ev();
        }

        #endregion

        #region Code Injection resources

        string NativeAnonymousDelegateSupportCode = @"if (typeof ($CreateAnonymousDelegate) == 'undefined') {
    var $CreateAnonymousDelegate = function (target, func) {
        if (target == null || func == null)
            return func;
        var delegate = function () {
            return func.apply(target, arguments);
        };
        delegate.func = func;
        delegate.target = target;
        delegate.isDelegate = true;
        return delegate;
    }
}
";
        string NativeDelegateSupportCode = @"if (typeof($CreateDelegate)=='undefined'){
    if(typeof($iKey)=='undefined') var $iKey = 0;
    if(typeof($pKey)=='undefined') var $pKey = String.fromCharCode(1);
    var $CreateDelegate = function(target, func){
        if (target == null || func == null) 
            return func;
        if(func.target==target && func.func==func)
            return func;
        if (target.$delegateCache == null)
            target.$delegateCache = {};
        if (func.$key == null)
            func.$key = $pKey + String(++$iKey);
        var delegate;
        if(target.$delegateCache!=null)
            delegate = target.$delegateCache[func.$key];
        if (delegate == null){
            delegate = function(){
                return func.apply(target, arguments);
            };
            delegate.func = func;
            delegate.target = target;
            delegate.isDelegate = true;
            if(target.$delegateCache!=null)
                target.$delegateCache[func.$key] = delegate;
        }
        return delegate;
    }
}
";
        string NativeExtensionDelegateSupportCode = @"if (typeof($CreateExtensionDelegate)=='undefined'){
    if(typeof($iKey)=='undefined') var $iKey = 0;
    if(typeof($pKey)=='undefined') var $pKey = String.fromCharCode(1);
    var $CreateExtensionDelegate = function(target, func){
        if (target == null || func == null) 
            return func;
        if(func.target==target && func.func==func)
            return func;
        if (target.$delegateCache == null)
            target.$delegateCache = {};
        if (func.$key == null)
            func.$key = $pKey + String(++$iKey);
        var delegate;
        if(target.$delegateCache!=null)
            delegate = target.$delegateCache[func.$key];
        if (delegate == null){
            delegate = function(){
                var args = [target];
                for(var i=0;i<arguments.length;i++)
                    args.push(arguments[i]);
                return func.apply(null, args);
            };
            delegate.func = func;
            delegate.target = target;
            delegate.isDelegate = true;
            delegate.isExtensionDelegate = true;
            if(target.$delegateCache!=null)
                target.$delegateCache[func.$key] = delegate;
        }
        return delegate;
    }
}
";

        //        string NativeInheritanceSupportCode =
        //@"if (typeof($Inherit)=='undefined') {
        //    var $Inherit = function(ce, ce2) {
        //        for (var p in ce2.prototype)
        //            if (typeof(ce.prototype[p]) == 'undefined' || ce.prototype[p]==Object.prototype[p])
        //                ce.prototype[p] = ce2.prototype[p];
        //        for (var p in ce2)
        //            if (typeof(ce[p]) == 'undefined')
        //                ce[p] = ce2[p];
        //        ce.$baseCtor = ce2;
        //    }
        //}";

        string NativeInheritanceSupportCode =
@"if (typeof ($Inherit) == 'undefined') {
	var $Inherit = function (ce, ce2) {

		if (typeof (Object.getOwnPropertyNames) == 'undefined') {

			for (var p in ce2.prototype)
				if (typeof (ce.prototype[p]) == 'undefined' || ce.prototype[p] == Object.prototype[p])
					ce.prototype[p] = ce2.prototype[p];
			for (var p in ce2)
				if (typeof (ce[p]) == 'undefined')
					ce[p] = ce2[p];
			ce.$baseCtor = ce2;

		} else {

			var props = Object.getOwnPropertyNames(ce2.prototype);
			for (var i = 0; i < props.length; i++)
				if (typeof (Object.getOwnPropertyDescriptor(ce.prototype, props[i])) == 'undefined')
					Object.defineProperty(ce.prototype, props[i], Object.getOwnPropertyDescriptor(ce2.prototype, props[i]));

			for (var p in ce2)
				if (typeof (ce[p]) == 'undefined')
					ce[p] = ce2[p];
			ce.$baseCtor = ce2;

		}

	}
};
";


        string CreateExceptionSupportCode =
@"if (typeof($CreateException)=='undefined') 
{
    var $CreateException = function(ex, error) 
    {
        if(error==null)
            error = new Error();
        if(ex==null)
            ex = new System.Exception.ctor();       
        error.message = ex.message;
        for (var p in ex)
           error[p] = ex[p];
        return error;
    }
}
";

        string CreateAnonymousObjectSupportCode =
@"if (typeof($CreateAnonymousObject)=='undefined') 
{
    var $CreateAnonymousObject = function(json)
    {
        var obj = new System.Object.ctor();
        obj.d = json;
        for(var p in json){
            obj['get_'+p] = new Function('return this.d.'+p+';');
        }
        return obj;
    }
}
";


        string RemoveDelegateSupportCode =
@"function $RemoveDelegate(delOriginal,delToRemove)
{
    if(delToRemove == null || delOriginal == null)
        return delOriginal;
    if(delOriginal.isMulticastDelegate)
    {
        if(delToRemove.isMulticastDelegate)
            throw new Error(""Multicast to multicast delegate removal is not implemented yet"");
        var del=$CreateMulticastDelegateFunction();
        for(var i=0;i < delOriginal.delegates.length;i++)
        {
            var del2=delOriginal.delegates[i];
            if(del2 != delToRemove)
            {
                if(del.delegates == null)
                    del.delegates = [];
                del.delegates.push(del2);
            }
        }
        if(del.delegates == null)
            return null;
        if(del.delegates.length == 1)
            return del.delegates[0];
        return del;
    }
    else
    {
        if(delToRemove.isMulticastDelegate)
            throw new Error(""single to multicast delegate removal is not supported"");
        if(delOriginal == delToRemove)
            return null;
        return delOriginal;
    }
};
";
        string CombineDelegatesSupportCode =
        @"function $CombineDelegates(del1,del2)
{
    if(del1 == null)
        return del2;
    if(del2 == null)
        return del1;
    var del=$CreateMulticastDelegateFunction();
    del.delegates = [];
    if(del1.isMulticastDelegate)
    {
        for(var i=0;i < del1.delegates.length;i++)
            del.delegates.push(del1.delegates[i]);
    }
    else
    {
        del.delegates.push(del1);
    }
    if(del2.isMulticastDelegate)
    {
        for(var i=0;i < del2.delegates.length;i++)
            del.delegates.push(del2.delegates[i]);
    }
    else
    {
        del.delegates.push(del2);
    }
    return del;
};
";
        string CreateMulticastDelegateFunctionSupportCode =
        @"function $CreateMulticastDelegateFunction()
{
    var del2 = null;
    
    var del=function()
    {
        var x=undefined;
        for(var i=0;i < del2.delegates.length;i++)
        {
            var del3=del2.delegates[i];
            x = del3.apply(null,arguments);
        }
        return x;
    };
    del.isMulticastDelegate = true;
    del2 = del;   
    
    return del;
};
";
        #endregion



        #region ICompiler Members


        public event Action<IEntity> BeforeConvertCsToTargetEntity;

        public event Action<IEntity, ITargetNode> AfterConvertCsToTargetEntity;



        public event Action BeforeExit;



        public event Action<ICSharpCode.NRefactory.CSharp.AstNode> BeforeConvertCsToTargetAstNode;

        public event Action<ICSharpCode.NRefactory.CSharp.AstNode, ITargetNode> AfterConvertCsToTargetAstNode;

        public event Action<ICSharpCode.NRefactory.Semantics.ResolveResult> BeforeConvertCsToTargetResolveResult;

        public event Action<ICSharpCode.NRefactory.Semantics.ResolveResult, ITargetNode> AfterConvertCsToTargetResolveResult;


        CustomAttributeProvider _CustomAttributeProvider;
        public ICustomAttributeProvider CustomAttributeProvider
        {
            get { return _CustomAttributeProvider; }
        }

        #endregion

        Lazy<JsExportAttribute> _JsExportAttribute;
        private JsStatement CreateAnonymousObjectSupportStatement;
        public JsExportAttribute GetJsExportAttribute()
        {
            if (_JsExportAttribute == null)
            {
                _JsExportAttribute = new Lazy<JsExportAttribute>(() => Sk.GetJsExportAttribute(Project.Compilation));
            }
            return _JsExportAttribute.Value;
        }

    }



}
