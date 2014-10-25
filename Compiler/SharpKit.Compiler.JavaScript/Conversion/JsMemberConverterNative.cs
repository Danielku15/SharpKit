using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.Extensions;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler.JavaScript.Ast;
using SharpKit.Compiler.JavaScript.Utils;
using SharpKit.Compiler.Utils;

namespace SharpKit.Compiler.JavaScript.Conversion
{
    class JsMemberConverterNative : JsMemberConverter
    {
        public override JsNode _VisitClass(ITypeDefinition ce)
        {
            var unit = new JsUnit { Statements = new List<JsStatement>() };
            ExportTypeNamespace(unit, ce);
            var members = GetMembersToExport(ce);

            var prototypeValue = Js.Json();
            prototypeValue.NamesValues = new List<JsJsonNameValue>();
            var nonStatics = new List<JsNode>();
            var statics = new List<JsNode>();

            foreach (var member in members)
            {
                if (member.IsStatic)
                {
                    statics.Add(Visit(member));
                }
                else
                {
                    var node = Visit(member);
                    IEnumerable<JsNode> toAdd = node.NodeType == JsNodeType.NodeList
                        ? ((JsNodeList)node).Nodes.AsEnumerable()
                        : new[] { node };
                    foreach (var jsNode in toAdd)
                    {
                        if (jsNode.NodeType == JsNodeType.JsonNameValue)
                        {
                            prototypeValue.NamesValues.Add((JsJsonNameValue)jsNode);
                        }
                        else
                        {
                            nonStatics.Add(jsNode);
                        }
                    }
                }
            }

            ImportToUnit(unit, nonStatics);
            if (prototypeValue.NamesValues.Count > 0)
            {
                var prototype = ExportTypePrefix(ce, false).Assign(prototypeValue);
                unit.Statements.Add(prototype.Statement());
            }
            ImportToUnit(unit, statics);

            var baseCe = ce.GetBaseTypeDefinition();
            if (baseCe != null && Utils.Sk.IsNativeType(baseCe) && !Utils.Sk.IsGlobalType(baseCe) && !Utils.Sk.OmitInheritance(ce))
            {
                unit.Statements.Add(Js.Member("$Inherit").Invoke(SkJs.EntityToMember(ce), SkJs.EntityToMember(baseCe)).Statement());
            }
            return unit;
        }

        public override JsNode _VisitEnum(ITypeDefinition ce)
        {
            var unit = new JsUnit { Statements = new List<JsStatement>() };
            ExportTypeNamespace(unit, ce);
            var json = VisitEnumToJson(ce);
            var typeName = GetJsTypeName(ce);
            var st = Js.Members(typeName).Assign(json).Statement();

            unit.Statements.Add(st);
            return unit;
        }

        public override JsNode _VisitField(IField fld)
        {
            var init2 = GetCreateFieldInitializer(fld);
            var initializer = AstNodeConverter.VisitExpression(init2);
            if (initializer != null)
            {
                var member = ExportTypePrefix(fld.GetDeclaringTypeDefinition(), fld.IsStatic());
                member = member.Member(fld.Name);
                var st2 = member.Assign(initializer).Statement();
                return st2;
            }
            return null;
        }

        JsMemberConverterGlobal CreateGlobalMemberConverter()
        {
            return new JsMemberConverterGlobal
            {
                Compiler = Compiler,
                AssemblyName = AssemblyName,
                AstNodeConverter = AstNodeConverter,
                Log = Log,
                LongFunctionNames = LongFunctionNames
            };
        }

        public override JsNode ExportConstructor(IMethod ctor)
        {
            if (ctor.IsStatic)
            {
                var globaImporter = CreateGlobalMemberConverter();
                var node2 = globaImporter.ExportConstructor(ctor);
                return node2;
            }
            else
            {
                var func = (JsFunction)base.ExportConstructor(ctor);
                var fullname = GetJsTypeName(ctor.GetDeclaringTypeDefinition());
                if (fullname.Contains("."))
                {
                    var st = Js.Members(fullname).Assign(func).Statement();
                    return st;
                }
                else
                {
                    return Js.Var(fullname, func).Statement();
                }
            }
        }

        public override JsNode ExportMethod(IMethod me)
        {
            if (Utils.Sk.IsGlobalMethod(me))
            {
                return CreateGlobalMemberConverter().ExportMethod(me);
            }
            var node = base.ExportMethod(me);
            if (node == null)
                return node;
            if (!node.Is(JsNodeType.Function))
                return node;

            var func = (JsFunction)node;
            func.Name = null;
            if (LongFunctionNames)
                func.Name = SkJs.GetLongFunctionName(me);

            if (me.IsStatic)
            {
                var ce = me.GetDeclaringTypeDefinition();
                var member = ExportTypePrefix(ce, me.IsStatic);
                member = member.Member(SkJs.GetEntityJsName(me));
                var st = member.Assign(func).Statement();
                return st;
            }
            return Js.JsonNameValue(SkJs.GetEntityJsName(me), func);
        }

        public override JsNode _Visit(IProperty pe)
        {
            var list = GetAccessorsToExport(pe);
            if (Utils.Sk.IsNativeProperty(pe))
            {
                var nodes = new JsNodeList
                {
                    Nodes = list.Select(ExportMethod).ToList()
                };


                var json = new JsJsonObjectExpression();
                foreach (var accessor in list)
                {
                    if (accessor == pe.Getter)
                        json.Add("get", ExportTypePrefix(pe.Getter.GetDeclaringTypeDefinition(), pe.IsStatic).Member("get_" + pe.Name));
                    if (accessor == pe.Setter)
                        json.Add("set", ExportTypePrefix(pe.Setter.GetDeclaringTypeDefinition(), pe.IsStatic).Member("set_" + pe.Name));
                }

                if (Utils.Sk.IsNativePropertyEnumerable(pe))
                    json.Add("enumerable", Js.True());

                var defineStatement = Js.Member("Object").Member("defineProperty").Invoke(
                    ExportTypePrefix(pe.GetDeclaringTypeDefinition(), pe.IsStatic),
                    Js.String(pe.Name),
                    json).Statement();

                nodes.Nodes.Add(defineStatement);

                return nodes;
            }
            else
            {
                return new JsNodeList
                {
                    Nodes = list.Select(ExportMethod).ToList()
                };
            }
        }

        #region Utils
        void ExportNamespace(JsUnit unit, string ns)
        {
            var Writer = new StringWriter();
            if (ns.IsNotNullOrEmpty())
            {
                var tokens = ns.Split('.');
                for (var i = 0; i < tokens.Length; i++)
                {
                    var ns2 = tokens.Take(i + 1).StringJoin(".");
                    JsStatement st;
                    if (i == 0)
                        st = Js.Var(ns2, Js.Member(ns2).Or(Js.Json())).Statement();
                    else
                        st = Js.Member(ns2).Assign(Js.Member(ns2).Or(Js.Json())).Statement();
                    unit.Statements.Add(st);
                    st.AddAnnotation(new NamespaceVerificationAnnotation { Namespace = ns2 });//.Ex(true).NamespaceVerification = ns2;
                }
            }
        }
        void ExportTypeNamespace(JsUnit unit, ITypeDefinition ce)
        {
            var name = GetJsTypeName(ce);
            if (name.IsNotNullOrEmpty() && name.Contains("."))
            {
                var ns = name.Split('.');
                ns = ns.Take(ns.Length - 1).ToArray();
                ExportNamespace(unit, ns.StringConcat("."));
            }
        }
        JsMemberExpression ExportTypePrefix(ITypeDefinition ce, bool isStatic)
        {
            var me = Js.Members(GetJsTypeName(ce));
            if (!isStatic)
                me = me.MemberOrSelf(Utils.Sk.GetPrototypeName(ce));
            return me;
        }

        #endregion
    }
}