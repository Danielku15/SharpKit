using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ICSharpCode.NRefactory.CSharp;
using SharpKit.Targets.JavaScript;
using Mono.Cecil;
using SharpKit.Compiler;
using SharpKit.Targets.JavaScript.Ast;

namespace SharpKit.Targets.JavaScript
{
    class JavaScriptTarget : CompilerTargetBase
    {
        public static string[] TypedArrays =
        {
            "Int8Array",
            "Uint8Array",
            "Int16Array",
            "Uint16Array",
            "Int32Array",
            "Uint32Array",
            "Float32Array",
            "Float64Array"
        };

        private JsFileMerger _jsFileMerger;

        private readonly List<CodeInjection> _codeInjections;
        private readonly List<string> _embeddedResourceFiles;

        public override CompilerTarget Target
        {
            get { return CompilerTarget.JavaScript; }
        }

        public override IEnumerable<string> ManifestFiles
        {
            get
            {
                return _jsFileMerger.ExternalFiles.Select(e => e.TargetFile.Filename).Concat(_embeddedResourceFiles);
            }
        }

        public SkFile CodeInjectionFile { get; set; }

        public JavaScriptTarget()
        {
            _embeddedResourceFiles = new List<string>();

            JsStatement nativeAnonymousDelegateSupportStatement = Js.CodeStatement(NativeAnonymousDelegateSupportCode);
            JsStatement nativeDelegateSupportStatement = Js.CodeStatement(NativeDelegateSupportCode);
            JsStatement nativeInheritanceSupportStatement = Js.CodeStatement(NativeInheritanceSupportCode);
            JsStatement nativeExtensionDelegateSupportStatement = Js.CodeStatement(NativeExtensionDelegateSupportCode);
            JsStatement combineDelegatesSupportStatement = Js.CodeStatement(CombineDelegatesSupportCode);
            JsStatement removeDelegateSupportStatement = Js.CodeStatement(RemoveDelegateSupportCode);
            JsStatement createMulticastDelegateFunctionSupportStatement = Js.CodeStatement(CreateMulticastDelegateFunctionSupportCode);
            JsStatement createExceptionSupportStatement = Js.CodeStatement(CreateExceptionSupportCode);
            JsStatement createAnonymousObjectSupportStatement = Js.CodeStatement(CreateAnonymousObjectSupportCode);

            _codeInjections = new List<CodeInjection>
            {
                new CodeInjection
                {
                    JsCode = NativeAnonymousDelegateSupportCode,
                    JsStatement = nativeAnonymousDelegateSupportStatement,
                    FunctionName = "$CreateAnonymousDelegate",
                },
                new CodeInjection
                {
                    JsCode = NativeDelegateSupportCode,
                    JsStatement = nativeDelegateSupportStatement,
                    FunctionName = "$CreateDelegate",
                },
                new CodeInjection
                {
                    JsCode = NativeInheritanceSupportCode,
                    JsStatement = nativeInheritanceSupportStatement,
                    FunctionName = "$Inherit",
                },
                new CodeInjection
                {
                    JsCode = NativeExtensionDelegateSupportCode,
                    JsStatement = nativeExtensionDelegateSupportStatement,
                    FunctionName = "$CreateExtensionDelegate",
                },
                new CodeInjection
                {
                    JsCode = CreateExceptionSupportCode,
                    JsStatement = createExceptionSupportStatement,
                    FunctionName = "$CreateException",
                },
                new CodeInjection
                {
                    JsCode = CreateMulticastDelegateFunctionSupportCode,
                    JsStatement = createMulticastDelegateFunctionSupportStatement,
                    FunctionName = "$CreateMulticastDelegateFunction",
                },
                new CodeInjection
                {
                    JsCode = CombineDelegatesSupportCode,
                    JsStatement = combineDelegatesSupportStatement,
                    FunctionName = "$CombineDelegates",
                },
                new CodeInjection
                {
                    JsCode = RemoveDelegateSupportCode,
                    JsStatement = removeDelegateSupportStatement,
                    FunctionName = "$RemoveDelegate",
                },
                new CodeInjection
                {
                    JsCode = CreateAnonymousObjectSupportCode,
                    JsStatement = createAnonymousObjectSupportStatement,
                    FunctionName = "$CreateAnonymousObject",
                },
            };

            AddCodeInjectionDependency("$CombineDelegates", "$CreateMulticastDelegateFunction");
            AddCodeInjectionDependency("$RemoveDelegate", "$CreateMulticastDelegateFunction");

            foreach (var ta in TypedArrays)
            {
                var define = Js.If(Js.Typeof(Js.Member(ta)).Equal(Js.Value("undefined")), Js.Var(ta, Js.Member("Array")).Statement());
                _codeInjections.Add(new CodeInjection
                {
                    FunctionName = ta,
                    JsStatement = define,
                });
            }
        }

        public override ICsExternalMetadata BuildExternalMetadata()
        {
            return new JsExternalMetadata();
        }

        public override string OutputSuffix
        {
            get { return ".js"; }
        }

        public override ITypeConverter BuildTypeConverter()
        {
            var typeConverter = new JsTypeConverter();
            var att = Compiler.GetJsExportAttribute();
            if (att != null)
            {
                typeConverter.ExportComments = att.ExportComments;
                //LongFunctionNames = att.LongFunctionNames;
                //Minify = att.Minify;
                //EnableProfiler = att.EnableProfiler;
            }
            return typeConverter;
        }

        #region Merge Files

        public override void MergeTargetFiles()
        {
            _jsFileMerger = new JsFileMerger
            {
                Project = Compiler.Project,
                Files = Compiler.SkFiles.OfType<SkJsFile>().ToList(),
                Log = Compiler.Log,
                Compiler = Compiler
            };

            Time(GenerateCodeInjectionFile);

            _jsFileMerger.MergeFiles();
            if (Compiler.Settings.OutputGeneratedJsFile.IsNotNullOrEmpty())
            {
                var file = _jsFileMerger.GetJsFile(Compiler.Settings.OutputGeneratedJsFile, false);
                _jsFileMerger.MergeFiles(file, Compiler.SkFiles.OfType<SkJsFile>().Where(t => t != file).ToList());
                Compiler.SkFiles.Clear();
                Compiler.SkFiles.Add(file);
            }

            ApplyJsMinifyAndSourceMap();
        }

        private void ApplyJsMinifyAndSourceMap()
        {
            var att = Compiler.Project.Compilation.MainAssembly.GetMetadata<JsExportAttribute>();
            if (att != null)
            {
                if (att.Minify)
                    Compiler.SkFiles.OfType<SkJsFile>().ForEach(t => t.Minify = true);
                if (att.GenerateSourceMaps)
                    Compiler.SkFiles.OfType<SkJsFile>().ForEach(t => t.GenerateSourceMap = true);
            }
        }

        private void GenerateCodeInjectionFile()
        {
            var att = Compiler.Project.Compilation.MainAssembly.GetMetadata<JsExportAttribute>();
            if (att != null && att.CodeInjectionFilename.IsNotNullOrEmpty())
            {
                CodeInjectionFile = _jsFileMerger.GetJsFile(att.CodeInjectionFilename, false);
                var headerUnit = CreateSharpKitHeaderUnit();
                foreach (var ci in _codeInjections)
                {
                    headerUnit.Statements.Add(ci.JsStatement);
                }
                ((SkJsFile)CodeInjectionFile).TargetFile.Units.Insert(0, headerUnit);
            }
        }

        private void AddCodeInjectionDependency(string funcName, string depFuncName)
        {
            var func = _codeInjections.FirstOrDefault(t => t.FunctionName == funcName);
            var dep = _codeInjections.FirstOrDefault(t => t.FunctionName == depFuncName);
            if (func != null && dep != null)
                func.Dependencies.Add(dep);
        }

        private JsUnit CreateSharpKitHeaderUnit()
        {
            var unit = new JsUnit { Statements = new List<JsStatement>() };
            var att = Compiler.GetJsExportAttribute();
            if (att == null || !att.OmitSharpKitHeaderComment)
            {
                var txt = " Generated by SharpKit 5 v" + Compiler.SkcVersion + " ";
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

        #endregion

        #region Inject Code

        public override void InjectTargetCode()
        {
            if (Compiler.SkFiles.IsNotNullOrEmpty())
            {
                var helperMethods = _codeInjections.Select(t => t.FunctionName).ToArray();
                foreach (var file in Compiler.SkFiles.OfType<SkJsFile>())
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
                                var ci = _codeInjections.FirstOrDefault(t => t.FunctionName == funcName);
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

        }

        private HashSet<string> CheckHelperMethodsUsage(SkJsFile file, string[] methods)
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

        #endregion

        #region Optimize

        public override void OptimizeTargetFiles()
        {
            OptimizeClrJsTypesArrayVerification();
            OptimizeNamespaceVerification();
        }

        private void OptimizeClrJsTypesArrayVerification()
        {
            if (Compiler.TypeConverter == null || ((JsTypeConverter)Compiler.TypeConverter).ClrConverter == null)
                return;
            var st = ((JsTypeConverter)Compiler.TypeConverter).ClrConverter.VerifyJsTypesArrayStatement;
            if (st == null)
                return;
            if (Compiler.SkFiles.IsNullOrEmpty())
                return;
            foreach (var file in Compiler.SkFiles.OfType<SkJsFile>())
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

        private void OptimizeNamespaceVerification()
        {
            if (Compiler.SkFiles.IsNullOrEmpty())
                return;
            foreach (var file in Compiler.SkFiles.OfType<SkJsFile>())
            {
                if (file.TargetFile == null || file.TargetFile.Units.IsNullOrEmpty())
                    continue;
                foreach (var unit in file.TargetFile.Units)
                {
                    OptimizeNamespaceVerification(unit);
                }
            }
        }

        private void OptimizeNamespaceVerification(JsUnit unit)
        {
            if (unit.Statements.IsNullOrEmpty())
                return;
            unit.Statements.RemoveDoublesByKey(GetNamespaceVerification);
        }

        private string GetNamespaceVerification(JsStatement st)
        {
            var ex = st.Annotation<NamespaceVerificationAnnotation>();
            if (ex != null)
                return ex.Namespace;
            return null;
        }

        #endregion

        #region Saving

        public override void SaveTargetFiles()
        {
            var att = Compiler.GetJsExportAttribute();
            string format = null;
            if (att != null)
            {
                format = att.JsCodeFormat;
            }

            foreach (var file in Compiler.SkFiles)
            {
                file.Format = format;
                file.Save();
            }
        }

        public override void EmbedResources()
        {
            var atts = Compiler.Project.Compilation.MainAssembly.GetMetadatas<JsEmbeddedResourceAttribute>().ToList();
            if (atts.IsNotNullOrEmpty())
            {
                var asmFilename = Compiler.Settings.Output;
                Compiler.Log.WriteLine("Loading assembly {0}", asmFilename);
                var asm = ModuleDefinition.ReadModule(asmFilename);
                var changed = false;
                foreach (var att in atts)
                {
                    if (att.Filename.IsNullOrEmpty())
                        throw new CompilerException(att.SourceAttribute, "JsEmbeddedResourceAttribute.Filename must be set");
                    _embeddedResourceFiles.Add(att.Filename);
                    var resName = att.ResourceName ?? att.Filename;
                    Compiler.Log.WriteLine("Embedding {0} -> {1}", att.Filename, resName);
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
                    var prms = new WriterParameters();//TODO:StrongNameKeyPair = new StrongNameKeyPair("Foo.snk") };
                    var snkFile = Compiler.Settings.NoneFiles.FirstOrDefault(t => t.EndsWith(".snk", StringComparison.InvariantCultureIgnoreCase));
                    if (snkFile != null)
                    {
                        Compiler.Log.WriteLine("Signing assembly with strong-name keyfile: {0}", snkFile);
                        prms.StrongNameKeyPair = new StrongNameKeyPair(snkFile);
                    }
                    Compiler.Log.WriteLine("Saving assembly {0}", asmFilename);
                    asm.Write(asmFilename, prms);
                }
            }
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

    }
}
