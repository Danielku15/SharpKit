using System;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.Extensions;
using ICSharpCode.NRefactory.TypeSystem;
using SharpKit.Compiler;

namespace SharpKit.Targets.Java
{
    static class JMeta
    {
        public static string DirectorySeparator = Path.DirectorySeparatorChar.ToString();

        //static JExportAttribute _JsExportAttribute;
        //public static JExportAttribute GetJExportAttribute()
        //{
        //    if (_JsExportAttribute == null)
        //    {
        //        _JsExportAttribute = GetJExportAttribute(CompilerTool.Current.Project.Compilation.MainAssembly);
        //    }
        //    return _JsExportAttribute;
        //}
        public static JExportAttribute GetJExportAttribute(IAssembly asm)
        {
            return asm.GetMetadata<JExportAttribute>();
        }

        public static string GetExportPath(ITypeDefinition ce)
        {
            var att = ce.GetJTypeAttribute();
            string path;
            if (att != null && att.Filename.IsNotNullOrEmpty())
            {
                path = att.Filename.Replace("/", JMeta.DirectorySeparator);
                if (path.StartsWith(@"~\") || path.StartsWith(@"~/"))
                    path = path.Substring(2);
                else
                    path = Path.Combine(Path.GetDirectoryName(ce.GetFileOrigin()), path);
                var asm = ce.ParentAssembly;
                var att2 = asm.GetMetadata<JExportAttribute>();
                if (att2 != null && att2.FilenameFormat.IsNotNullOrEmpty())
                    path = String.Format(att2.FilenameFormat, path);
            }
            else
            {
                path = GetDefaultJFilename(ce);
            }
            return path;
        }
        private static string GetDefaultJFilename(ITypeDefinition ce)
        {
            var asm = ce.ParentAssembly;
            var s = "res" + JMeta.DirectorySeparator + asm.AssemblyName + ".java";
            var att = asm.GetMetadata<JExportAttribute>();
            if (att != null)
            {
                if (att.DefaultFilename.IsNotNullOrEmpty())
                {
                    s = att.DefaultFilename;
                }
                else if (att.DefaultFilenameAsCsFilename)
                {
                    var filename = ce.GetFileOrigin();
                    filename = Path.ChangeExtension(filename, ".java");
                    if (att.FilenameFormat.IsNotNullOrEmpty())
                        filename = String.Format(att.FilenameFormat, filename);
                    s = filename;
                }
            }
            return s.Replace("/", JMeta.DirectorySeparator);
        }


        #region JMethodAttribute
        public static JMethodAttribute GetJMethodAttribute(IMethod me)
        {
            if (me == null)
                return null;
            return me.GetMetadata<JMethodAttribute>(true);
        }
        public static bool UseNativeOverloads(IMethod me)
        {
            if (me.IsPropertyAccessor())
                return true;
            if (me.IsEventAccessor())
                return true;
            JMethodAttribute jma = me.GetMetadata<JMethodAttribute>(true);
            if (jma != null && jma._NativeOverloads != null)
                return jma._NativeOverloads.GetValueOrDefault();

            var t = me.GetDeclaringTypeDefinition();
            if (t != null)
            {
                return UseNativeOverloads(t);
            }
            else
            {
                return false; //Not declared on method, not declared on type
            }

        }
        public static string GetNativeCode(IMethod me)
        {
            JMethodAttribute jma = me.GetMetadata<JMethodAttribute>();
            return (jma == null) ? null : jma.Code;
        }
        public static bool ExtensionImplementedInInstance(IMethod me)
        {
            JMethodAttribute jma = me.GetMetadata<JMethodAttribute>();
            return (jma == null) ? false : jma.ExtensionImplementedInInstance;
        }
        public static bool IgnoreGenericMethodArguments(IMethod me)
        {
            if (me == null)
                return false;
            return MD_JMethodOrJType(me, t => t._IgnoreGenericArguments, t => t._IgnoreGenericMethodArguments).GetValueOrDefault();
        }
        public static bool IsGlobalMethod(IMethod me)
        {
            var att = me.GetMetadata<JMethodAttribute>(true);
            if (att != null && att._Global != null)
                return att._Global.Value;
            var owner = me.GetOwner();
            if (owner != null && owner is IProperty)
            {
                return IsGlobalProperty((IProperty)owner);
            }
            return IsGlobalType(me.GetDeclaringTypeDefinition());

        }

        #endregion

        #region JEventAttribute
        public static JEventAttribute GetJsEventAttribute(IEntity me) //TODO: implement
        {
            return me.GetMetadata<JEventAttribute>();
        }
        #endregion

        #region JPropertyAttribute
        public static JPropertyAttribute GetJPropertyAttribute(IProperty pe)
        {
            return pe.GetMetadata<JPropertyAttribute>();
        }
        public static bool IsNativeField(IProperty pe)
        {
            var jpa = pe.GetMetadata<JPropertyAttribute>();
            if (jpa != null)
                return jpa.NativeField;
            var att = GetJTypeAttribute(pe.GetDeclaringTypeDefinition());//.GetMetadata<JsTypeAttribute>(true);
            if (att != null)
            {
                if (att.PropertiesAsFields)
                    return true;
                else if (att.AutomaticPropertiesAsFields && pe.IsAutomaticProperty())
                    return true;
                else
                    return false;
            }
            return false;
        }
        public static bool UseNativeIndexer(IProperty pe)
        {
            return pe.MD<JPropertyAttribute, bool>(t => t.NativeIndexer);
        }

        public static bool IsNativeProperty(IProperty pe)
        {
            if (IsNativeField(pe))
                return false;
            var x = MD_JPropertyOrJType(pe, t => t._NativeProperty, t => t._NativeProperties);
            return x.GetValueOrDefault();
            //var attr = GetJsPropertyAttribute(pe);
            //return attr != null && attr.NativeProperty;
        }

        public static bool IsNativePropertyEnumerable(IProperty pe)
        {
            var x = MD_JPropertyOrJType(pe, t => t._NativePropertyEnumerable, t => t._NativePropertiesEnumerable);
            return x.GetValueOrDefault(); ;
        }

        #endregion

        #region JExport

        public static bool IsJExported(IEntity me)
        {
            var ext = me.GetExtension(true);
            if (ext.IsExported == null)
            {
                ext.IsExported = IsJExported_Internal(me).GetValueOrDefault();
                //if (ext.IsExported == null)
                //{
                //    var decType = me.GetDeclaringTypeDefinition();
                //    if(decType!=null)
                //        ext.IsExported = IsExported(decType);
                //}
            }
            return ext.IsExported.Value;
        }

        private static bool? IsJExported_Internal(IEntity me)
        {
            if (me is ITypeDefinition)
            {
                var ce = (ITypeDefinition)me;
                return ce.MD_JType(t => t._Export).GetValueOrDefault(true);
            }
            if (me.SymbolKind == SymbolKind.Method || me.SymbolKind == SymbolKind.Accessor)
            {
                var me2 = (IMethod)me;
                return me2.MD_JMethodOrJType(t => t._Export, t => t._Export).GetValueOrDefault(true);
            }
            if (me.SymbolKind == SymbolKind.Property)
            {
                var pe = (IProperty)me;
                return pe.MD_JPropertyOrJType(t => t._Export, t => t._Export).GetValueOrDefault(true);
            }
            if (me.SymbolKind == SymbolKind.Field)//danel: || const
            {
                var pe = (IField)me;
                return pe.MD_JFieldOrJType(t => t._Export, t => t._Export).GetValueOrDefault(true);
            }
            //other entity types
            var decType = me.GetDeclaringTypeDefinition();
            if (decType != null)
                return IsJExported(decType);
            return null;
        }


        #endregion

        #region JType
        public static bool UseNativeJsons(ITypeDefinition type)
        {
            var att = type.GetJTypeAttribute();
            if (att != null && att.NativeJsons)
                return true;
            return false;
        }

        public static JTypeAttribute GetJTypeAttribute(this ITypeDefinition ce)
        {
            if (ce == null)
                return null;
            var att = ce.GetMetadata<JTypeAttribute>();
            if (att == null && ce.ParentAssembly != null)
                att = GetDefaultJTypeAttribute(ce);
            return att;
        }

        private static JTypeAttribute GetDefaultJTypeAttribute(ITypeDefinition ce)
        {
            if (ce == null)
                return null;
            return ce.ParentAssembly.GetMetadatas<JTypeAttribute>().Where(t => t.TargetType == null).FirstOrDefault();
        }
        public static bool UseNativeOperatorOverloads(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._NativeOperatorOverloads).GetValueOrDefault();
        }
        public static bool UseNativeOverloads(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._NativeOverloads).GetValueOrDefault();
        }
        public static bool IgnoreTypeArguments(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._IgnoreGenericTypeArguments).GetValueOrDefault();
        }
        public static bool IsGlobalType(ITypeDefinition ce)
        {
            if (ce == null)
                return false;
            var ext = ce.GetExtension(true);
            if (ext.IsGlobalType == null)
                ext.IsGlobalType = IsGlobalType_Internal(ce);
            return ext.IsGlobalType.Value;
        }

        public static bool IsGlobalType_Internal(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._GlobalObject).GetValueOrDefault();
        }

        public static bool IsClrType(ITypeDefinition ce)
        {
            if (ce == null)
                return false;
            return !IsNativeType(ce) && !IsGlobalType(ce);
        }

        public static bool IsNativeType(ITypeDefinition ce)
        {
            if (ce == null)
                return false;
            var ext = ce.GetExtension(true);
            if (ext.IsNativeType == null)
                ext.IsNativeType = IsNativeType_Internal(ce);
            return ext.IsNativeType.Value;
        }
        public static bool IsExtJType(ITypeDefinition ce)
        {
            var mode = ce.MD_JType(t => t._Mode);
            return mode != null && mode.Value == JMode.ExtJs;
        }

        public static bool IsNativeType_Internal(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._Native).GetValueOrDefault();
        }

        public static bool OmitDefaultConstructor(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._OmitDefaultConstructor).GetValueOrDefault();
        }


        #endregion

        #region JDelegate

        public static JDelegateAttribute GetJsDelegateAttribute(ITypeDefinition et)
        {
            if (et == null || !et.IsDelegate())
                return null;

            var data = et.GetMetadata<JDelegateAttribute>();
            return data;
        }

        #endregion

        #region Entity

        public static bool IsGlobalMember(IEntity me)
        {
            if (me is IMethod)
                return IsGlobalMethod((IMethod)me);
            if (me is ITypeDefinition)
                return IsGlobalType((ITypeDefinition)me);
            if (me is IProperty)
                return IsGlobalProperty((IProperty)me);
            return IsGlobalType(me.GetDeclaringTypeDefinition());

        }

        private static bool IsGlobalProperty(IProperty me)
        {
            var att = me.GetMetadata<JPropertyAttribute>(true);
            if (att != null && att._Global != null)
                return att._Global.Value;
            return IsGlobalType(me.GetDeclaringTypeDefinition());
        }

        #endregion

        public static ITypeDefinition GetBaseJClrType(ITypeDefinition ce)
        {
            var baseCe = ce.GetBaseTypeDefinition();
            while (baseCe != null && !IsClrType(baseCe))
                baseCe = baseCe.GetBaseTypeDefinition();
            return baseCe;
        }

        public static bool IsJsonMode(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._Mode) == JMode.Json;
        }

        //public static bool ForceDelegatesAsNativeFunctions(IEntity me)
        //{
        //    if(me is IMethod)
        //        return ForceDelegatesAsNativeFunctions((IMethod)me);
        //    else if (me is ITypeDefinition)
        //        return ForceDelegatesAsNativeFunctions(((ITypeDefinition)me));
        //    else if (me is IType)
        //        return ForceDelegatesAsNativeFunctions(((IType)me).GetDefinitionOrArrayType());
        //    return ForceDelegatesAsNativeFunctions(me.DeclaringTypeDefinition);
        //}
        public static bool ForceDelegatesAsNativeFunctions(IMethod me)
        {
            return me.MD_JMethodOrJType(t => t._ForceDelegatesAsNativeFunctions, t => t._ForceDelegatesAsNativeFunctions).GetValueOrDefault();
        }
        public static bool ForceDelegatesAsNativeFunctions(IMember me)
        {
            if (me is IMethod)
                return ForceDelegatesAsNativeFunctions((IMethod)me);
            ITypeDefinition ce;
            if (me is ITypeDefinition)
                ce = (ITypeDefinition)me;
            else
                ce = me.DeclaringTypeDefinition;

            return ce.MD_JType(t => t._ForceDelegatesAsNativeFunctions).GetValueOrDefault();
        }
        //public static bool ForceDelegatesAsNativeFunctions(ITypeDefinition ce)
        //{
        //    return ce.MD_JsType(t => t._ForceDelegatesAsNativeFunctions).GetValueOrDefault();
        //}

        public static bool InlineFields(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._InlineFields).GetValueOrDefault();
        }
        public static bool OmitInheritance(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._OmitInheritance).GetValueOrDefault();
        }

        public static bool OmitCasts(ITypeDefinition ce, SkProject project)
        {
            var att = GetJExportAttribute(project.Compilation.MainAssembly);
            if (att != null && att.ForceOmitCasts)
                return true;
            var value = ce.MD_JType(t => t._OmitCasts);
            return value.GetValueOrDefault();
        }

        public static bool OmitOptionalParameters(IMethod me)
        {
            return me.MD_JMethodOrJType(t => t._OmitOptionalParameters, t => t._OmitOptionalParameters).GetValueOrDefault();
        }

        //public static bool IsStructAsClass(IStruct ce)
        //{
        //    var att = GetJsStructAttribute(ce);
        //    if (att != null)
        //        return att.IsClass;
        //    return false;
        //}

        //private static JsStructAttribute GetJsStructAttribute(IStruct ce)
        //{
        //    return ce.GetMetadata<JsStructAttribute>();
        //}

        #region Utils

        static R MD<T, R>(this IEntity me, Func<T, R> selector) where T : System.Attribute
        {
            var att = me.GetMetadata<T>(true);
            if (att != null)
                return selector(att);
            return default(R);
        }
        static R MD_JMethod<R>(this IMethod me, Func<JMethodAttribute, R> func)
        {
            return me.MD(func);
        }
        static R MD_JProperty<R>(this IProperty me, Func<JPropertyAttribute, R> func)
        {
            return me.MD(func);
        }
        static R MD_JField<R>(this IField me, Func<JFieldAttribute, R> func)
        {
            return me.MD(func);
        }
        static R MD_JType<R>(this ITypeDefinition ce, Func<JTypeAttribute, R> func2)
        {
            var att = ce.GetMetadata<JTypeAttribute>();
            if (att != null)
            {
                var x = func2(att);
                if (((object)x) != null)
                    return x;
            }
            att = GetDefaultJTypeAttribute(ce);
            if (att != null)
                return func2(att);
            return default(R);
        }
        static R MD_JMethodOrJType<R>(this IMethod me, Func<JMethodAttribute, R> func, Func<JTypeAttribute, R> func2)
        {
            var x = me.MD_JMethod(func);
            if (((object)x) != null)
                return x;
            var ce = me.GetDeclaringTypeDefinition();
            if (ce != null)
                x = ce.MD_JType(func2);
            return x;
        }
        static R MD_JPropertyOrJType<R>(this IProperty me, Func<JPropertyAttribute, R> func, Func<JTypeAttribute, R> func2)
        {
            var x = me.MD_JProperty(func);
            if (((object)x) != null)
                return x;
            var ce = me.GetDeclaringTypeDefinition();
            if (ce != null)
                x = ce.MD_JType(func2);
            return x;
        }
        static R MD_JFieldOrJType<R>(this IField me, Func<JFieldAttribute, R> func, Func<JTypeAttribute, R> func2)
        {
            var x = me.MD_JField(func);
            if (((object)x) != null)
                return x;
            var ce = me.GetDeclaringTypeDefinition();
            if (ce != null)
                x = ce.MD_JType(func2);
            return x;
        }
        #endregion

        public static bool IsNativeParams(IMethod me)
        {
            var x = me.MD_JMethodOrJType(t => t._NativeParams, t => t._NativeParams);
            if (x == null)
                return true;
            return x.Value;
        }

        public static string GetPrototypeName(ITypeDefinition ce)
        {
            var att = GetJTypeAttribute(ce);
            if (att != null && att.PrototypeName != null)
                return att.PrototypeName;
            return "prototype";
        }

        public static bool IsNativeError(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._NativeError).GetValueOrDefault();
        }

        public static bool NativeCasts(ITypeDefinition ce)
        {
            return ce.MD_JType(t => t._NativeCasts).GetValueOrDefault();
        }

        public static string GetGenericArugmentJCode(ITypeDefinition ce)
        {
            return MD_JType(ce, t => t._GenericArgumentJCode);
        }


        /// <summary>
        /// Gets a member or a class, identifies if it's an enum member or type, 
        /// and returns whether this enum has no JsType attribute or has JsType(JsMode.Json)
        /// </summary>
        /// <param name="me"></param>
        /// <returns></returns>
        public static bool UseJsonEnums(IEntity me, out bool valuesAsNames)
        {
            if (me.IsEnumMember() || me.IsEnum())
            {
                var ce = me.IsEnum() ? (ITypeDefinition)me : me.GetDeclaringTypeDefinition();
                var use = true;
                var att = ce.GetJTypeAttribute();
                if (att != null)
                    use = att.Mode == JMode.Json;
                valuesAsNames = false;
                var att2 = ce.GetMetadata<JEnumAttribute>();
                if (att2 != null && att2._ValuesAsNames != null)
                {
                    valuesAsNames = att2 != null && att2.ValuesAsNames;
                }
                else if (ce.ParentAssembly != null)
                {
                    var att3 = ce.ParentAssembly.GetMetadata<JEnumAttribute>();
                    if (att3 != null)
                        valuesAsNames = att3.ValuesAsNames;
                }
                return use;
            }
            valuesAsNames = false;
            return false;
        }
    }
}