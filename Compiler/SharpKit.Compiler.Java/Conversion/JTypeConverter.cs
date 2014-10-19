using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Extensions;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.Plugin;
using SharpKit.Compiler.Targets.Ast;
using SharpKit.Compiler.Targets.Java.Ast;

namespace SharpKit.Compiler.Targets.Java
{
    class JTypeConverter : ITypeConverter
    {
        private List<TargetFile> _jsFiles;

        public bool ExportComments { get; set; }
        public ICsExternalMetadata ExternalMetadata { get; set; }
        public SkProject Project { get { return Compiler.Project; } }

        public IEnumerable<TargetFile> TargetFiles
        {
            get { return _jsFiles; }
        }

        public string AssemblyName { get; set; }
        public bool LongFunctionNames { get; set; }
        public Action<Dictionary<TargetFile, List<ITypeDefinition>>> BeforeExportTypes { get; set; }
        public ICompiler Compiler { get; set; }

        public event Action<IMemberConverter> ConfigureMemberConverter;

        JMemberConverterNative NativeConverter;

        private bool ShouldExportType(ITypeDefinition ce)
        {
            return JMeta.IsJExported(ce);
        }

        public List<ExportedType> ExportedTypes { get; set; }
        List<ITypeDefinition> GetExternalTypesToExport()
        {
            return null;
            //if (ExternalMetadata == null)
            //    return null;
            //var list2 = ExternalMetadata.TypesWithExternalAttributes.Where(ShouldExportType).ToList();
            //var list3 = new List<ITypeDefinition>();
            //foreach (var ce in list2)
            //{
            //    if (!CanExportExternalType(ce))
            //    {
            //        var att = ce.Attributes.FindByType<JTypeAttribute>().Where(t => t.GetDeclaration() != null).FirstOrDefault();
            //        if (att != null) //do not give warnings on reference assembly attributes.
            //            Log.Warn(att.GetDeclaration(), "Only enums and interfaces can be exported externally, set Export=false on this JsTypeAttribute");
            //    }
            //    else
            //    {
            //        list3.Add(ce);
            //    }
            //}
            //return list3;
        }
        public void Process()
        {
            var list = Project.MainAssembly.GetTypes().Where(ShouldExportType).ToList();
            var withNested = new List<ITypeDefinition>();
            foreach (var ce in list)
            {
                withNested.Add(ce);
                withNested.AddRange(ce.NestedTypes.Where(ShouldExportType));
            }
            list = withNested;
            var list2 = GetExternalTypesToExport();
            if (list2 != null)
                list.AddRange(list2);

            ExportedTypes = new List<ExportedType>();
            foreach (var ce in list)
            {
                var et = new ExportedType
                {
                    Type = ce,
                    ClassDeclaration = ExportType(ce),
                };
                ExportedTypes.Add(et);
            }

            //var byFile = list.GroupBy(Sk.GetExportPath).ToDictionary();

            //byFile.ForEach(t => SortByNativeInheritance(t.Value));
            //foreach (var f in byFile)
            //{
            //    var customOrder = f.Value.Where(t => GetOrderInFile(t) != 0).ToList();
            //    if (customOrder.Count > 0)
            //    {
            //        f.Value.RemoveAll(t => customOrder.Contains(t));
            //        customOrder.Sort((x, y) => GetOrderInFile(x) - GetOrderInFile(y));
            //        f.Value.InsertRange(0, customOrder.Where(t => GetOrderInFile(t) < 0));
            //        f.Value.AddRange(customOrder.Where(t => GetOrderInFile(t) > 0));
            //    }
            //}

            //var byFile2 = new Dictionary<JsFile, List<ITypeDefinition>>();
            //foreach (var pair in byFile)
            //{
            //    var file = new JsFile { Filename = pair.Key, Units = new List<JsUnit> { new JsUnit { Statements = new List<JsStatement>() } } };
            //    byFile2.Add(file, pair.Value);
            //}
            //if (BeforeExportTypes != null)
            //    BeforeExportTypes(byFile2);
            ////export by filenames and order
            //byFile2.ForEachParallel(ExportTypesInFile);

            //Files = byFile2.Keys.ToList();
        }

        //private void Test()
        //{
        //    var ce = new GenTypeDefinition { Name = "MyEnumerator" };
        //    var me = new GenMethod { Name = "Func1", ReturnType = this.Project.Compilation.FindType(KnownTypeCode.Boolean) };
        //    me.Declaration = new MethodDeclaration();
        //    var xx = Cs.Binary(Cs.Value(7), System.Linq.Expressions.ExpressionType.GreaterThan, Cs.Value(8), Cs.BooleanType());
        //    var st = Cs.If(xx, Cs.Member(Cs.This(ce), me).Invoke().Statement());
        //    me.Declaration.Body.Add(st);
        //    ce.Members.Add(me);
        //}

        private void ExportTypesInFile(KeyValuePair<JFile, List<ITypeDefinition>> p)
        {
            p.Value.ForEach(t => ExportType(t, p.Key));
        }

        private void SortByNativeInheritance(List<ITypeDefinition> list)
        {
            var list2 = list.Where(t => JMeta.IsNativeType(t) && t.GetBaseTypeDefinition() != null && JMeta.IsNativeType(t.GetBaseTypeDefinition())).ToList();
            foreach (var ce in list2)
            {
                var ce3 = ce;
                while (true)
                {
                    var baseCe = ce3.GetBaseTypeDefinition();
                    if (baseCe == null)
                        break;
                    MoveBefore(list, ce3, baseCe);
                    ce3 = baseCe;
                }
            }
        }

        private static void MoveBefore(List<ITypeDefinition> list, ITypeDefinition ce, ITypeDefinition baseCe)
        {
            var ceIndex = list.IndexOf(ce);
            var baseCeIndex = list.IndexOf(baseCe);
            if (baseCeIndex >= 0 && ceIndex >= 0)
            {
                if (baseCeIndex > ceIndex)
                {
                    list.RemoveAt(baseCeIndex);
                    list.Insert(ceIndex, baseCe);
                }
            }
        }


        void ExportType(ITypeDefinition ce, JFile jsFile)
        {
            var unit = ExportType(ce);
            var unit2 = new JCompilationUnit { PackageName = ce.GetPackageName(), Declarations = { unit } };
            jsFile.Units.Add(unit2);//[0].Statements.AddRange(unit2);

        }


        private JMemberConverterNative GetTypeImporter(ITypeDefinition ce)
        {
            JMemberConverterNative export;
            var isExtJs = JMeta.IsExtJType(ce);
            var isGlobal = JMeta.IsGlobalType(ce) && !isExtJs;
            var isNative = JMeta.IsNativeType(ce) && !isExtJs;
            isNative = true;
            isGlobal = false;
            isExtJs = false;
            if (isGlobal)
            {
                throw new NotSupportedException();
            }
            else if (isNative)
            {
                if (NativeConverter == null)
                    NativeConverter = new JMemberConverterNative { Compiler = Compiler };
                export = NativeConverter;
            }
            else if (isExtJs)
            {
                throw new NotSupportedException();
            }
            else
            {
                throw new NotSupportedException();
            }
            ConfigureTypeExport(export);
            return export;
        }

        private void ConfigureTypeExport(JMemberConverterNative typeExporter)
        {
            if (typeExporter.JCodeImporter == null)
            {
                typeExporter.AssemblyName = AssemblyName;
                typeExporter.JCodeImporter = new JCodeImporter { Log = Compiler.Log, ExportComments = ExportComments, Project=Project, Compiler=Compiler };
                typeExporter.LongFunctionNames = LongFunctionNames;
                typeExporter.Log = Compiler.Log;
                if (ConfigureMemberConverter != null)
                    ConfigureMemberConverter(typeExporter);
            }
        }

        JClassDeclaration ExportType(ITypeDefinition ce)
        {
            var att = SharpKit.Compiler.Java.Utils.Sk.GetJTypeAttribute(ce);
            //if (att != null && att.PreCode != null)
            //    unit.Statements.Add(Js.Code(att.PreCode).Statement());

            var isGlobal = att != null && att.GlobalObject;
            var isNative = att != null && att.Native;
            var isClr = !isGlobal && !isNative;


            var importer = GetTypeImporter(ce);
            //importer.Unit = new JsUnit { Statements = new List<JsStatement>() };
            var unit2 = (JClassDeclaration) importer.Visit(ce).Single();
            return unit2;
        }
    }


    class ExportedType
    {
        public JClassDeclaration ClassDeclaration { get; set; }
        public ITypeDefinition Type { get; set; }
    }
}
